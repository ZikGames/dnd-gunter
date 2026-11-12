using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace DNDHelper.ViewModels.Diary
{
    // Богатый текст для дневника поверх AvaloniaEdit.
    //
    // В Avalonia нет RichTextBox/FlowDocument/TextRange.Save(DataFormats.Rtf), поэтому здесь свой
    // минимальный слой: один Fmt на каждый символ документа (жирный, курсив, подчёркнутый,
    // зачёркнутый, размер), отрисовка через DocumentColorizingTransformer и собственные чтение и
    // запись подмножества RTF (то, что умел дневник в WPF-версии: b, i, ul, strike, fs, абзацы).
    // Списки, отступы, цвета и шрифты из старых .rtf при чтении отбрасываются (остаётся текст).

    /// <summary>Форматирование одного символа. Sz == 0 - размер по умолчанию (FontSize редактора).</summary>
    public readonly record struct Fmt(bool B, bool I, bool U, bool S, double Sz)
    {
        public bool IsDefault => !B && !I && !U && !S && Sz == 0;
    }

    /// <summary>Чтение и запись RTF (подмножество). Размеры: \fsN - полупункты, в WPF 1 px = 0.75 pt.</summary>
    public static class RtfIO
    {
        private static readonly HashSet<string> SkipDestinations = new()
        {
            "fonttbl", "colortbl", "stylesheet", "info", "pict", "listtable", "listoverridetable",
            "rsidtbl", "generator", "pntext", "listtext", "themedata", "colorschememapping",
            "latentstyles", "datastore", "xmlnstbl", "header", "footer", "footnote", "object"
        };

        private static readonly HashSet<string> UnderlineWords = new()
        {
            "ul", "uld", "uldb", "ulw", "ulth", "ulwave", "uldash", "uldashd", "uldashdd", "ulhwave", "ulldash"
        };

        private static readonly Dictionary<string, string> Symbols = new()
        {
            ["bullet"] = "\u2022",
            ["endash"] = "\u2013",
            ["emdash"] = "\u2014",
            ["lquote"] = "\u2018",
            ["rquote"] = "\u2019",
            ["ldblquote"] = "\u201c",
            ["rdblquote"] = "\u201d",
            ["emspace"] = " ",
            ["enspace"] = " ",
            ["qmspace"] = " "
        };

        private sealed class State
        {
            public Fmt F;
            public bool Skip;
        }

        private static bool IsAsciiLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        private static Encoding GetEncoding(int codePage)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(codePage);
            }
            catch
            {
                return Encoding.Latin1;
            }
        }

        /// <summary>Разбирает RTF в обычный текст + форматирование каждого символа.</summary>
        public static (string Text, Fmt[] Formats) Parse(string rtf)
        {
            var text = new StringBuilder();
            var fmts = new List<Fmt>();
            var stack = new Stack<State>();
            var cur = new State();
            int codePage = 1252;
            int uc = 1;
            int pendingSkip = 0;
            bool afterBrace = false;
            Encoding? enc = null;
            int i = 0;
            int n = rtf.Length;

            void Add(string s)
            {
                if (cur.Skip) return;
                foreach (char ch in s)
                {
                    text.Append(ch);
                    fmts.Add(cur.F);
                }
            }

            while (i < n)
            {
                char c = rtf[i];

                if (c == '{')
                {
                    stack.Push(cur);
                    cur = new State { F = cur.F, Skip = cur.Skip };
                    i++;
                    afterBrace = true;
                    continue;
                }

                if (c == '}')
                {
                    if (stack.Count > 0) cur = stack.Pop();
                    i++;
                    afterBrace = false;
                    continue;
                }

                // Переводы строк в самом файле RTF не являются содержимым
                if (c == '\r' || c == '\n')
                {
                    i++;
                    continue;
                }

                if (c == '\\')
                {
                    i++;
                    if (i >= n) break;
                    char d = rtf[i];

                    if (!IsAsciiLetter(d))
                    {
                        // управляющий символ: \\ \{ \} \' \* \~ \_ \- или обратная косая + перевод строки
                        i++;
                        switch (d)
                        {
                            case '\\':
                            case '{':
                            case '}':
                                if (pendingSkip > 0) pendingSkip--;
                                else Add(d.ToString());
                                break;
                            case '\'':
                                if (i + 1 < n &&
                                    byte.TryParse(rtf.AsSpan(i, 2), NumberStyles.AllowHexSpecifier,
                                        CultureInfo.InvariantCulture, out byte hex))
                                {
                                    i += 2;
                                    if (pendingSkip > 0)
                                    {
                                        pendingSkip--;
                                    }
                                    else
                                    {
                                        enc ??= GetEncoding(codePage);
                                        Add(enc.GetString(new[] { hex }));
                                    }
                                }
                                break;
                            case '*':
                                cur.Skip = true; // неизвестное назначение группы - пропускаем всю группу
                                break;
                            case '~':
                                Add("\u00A0");
                                break;
                            case '_':
                                Add("\u2011");
                                break;
                            case '\r':
                            case '\n':
                                Add("\n");
                                break;
                        }
                        afterBrace = false;
                        continue;
                    }

                    // управляющее слово
                    int start = i;
                    while (i < n && IsAsciiLetter(rtf[i])) i++;
                    string word = rtf.Substring(start, i - start);

                    int? param = null;
                    int beforeParam = i;
                    bool negative = false;
                    if (i < n && rtf[i] == '-')
                    {
                        negative = true;
                        i++;
                    }
                    int digitsStart = i;
                    while (i < n && rtf[i] >= '0' && rtf[i] <= '9') i++;
                    if (i > digitsStart)
                    {
                        if (int.TryParse(rtf.AsSpan(digitsStart, i - digitsStart), NumberStyles.None,
                                CultureInfo.InvariantCulture, out int value))
                            param = negative ? -value : value;
                    }
                    else if (negative)
                    {
                        i = beforeParam; // '-' не был знаком параметра
                    }
                    if (i < n && rtf[i] == ' ') i++; // один пробел после слова - разделитель

                    if (afterBrace && SkipDestinations.Contains(word)) cur.Skip = true;
                    afterBrace = false;

                    switch (word)
                    {
                        case "par":
                        case "line":
                            Add("\n");
                            break;
                        case "tab":
                            Add("\t");
                            break;
                        case "b":
                            cur.F = cur.F with { B = param != 0 };
                            break;
                        case "i":
                            cur.F = cur.F with { I = param != 0 };
                            break;
                        case "ulnone":
                            cur.F = cur.F with { U = false };
                            break;
                        case "strike":
                            cur.F = cur.F with { S = param != 0 };
                            break;
                        case "fs":
                            if (param.HasValue)
                                cur.F = cur.F with { Sz = Math.Round(param.Value / 1.5, 1) };
                            break;
                        case "plain":
                            cur.F = default;
                            break;
                        case "ansicpg":
                            if (param.HasValue)
                            {
                                codePage = param.Value;
                                enc = null;
                            }
                            break;
                        case "uc":
                            if (param.HasValue) uc = param.Value;
                            break;
                        case "u":
                            if (param.HasValue)
                            {
                                int code = param.Value < 0 ? param.Value + 65536 : param.Value;
                                Add(((char)(code & 0xFFFF)).ToString());
                                pendingSkip = uc; // запасной символ после \uN пропускаем
                            }
                            break;
                        default:
                            if (UnderlineWords.Contains(word))
                                cur.F = cur.F with { U = param != 0 };
                            else if (Symbols.TryGetValue(word, out string? sym))
                                Add(sym);
                            break;
                    }
                    continue;
                }

                // обычный символ
                if (pendingSkip > 0)
                {
                    pendingSkip--;
                    i++;
                    continue;
                }
                Add(c.ToString());
                i++;
                afterBrace = false;
            }

            // Размеры из файла оставляем как есть: так заметка из WPF-версии показывается тем же размером,
            // что был в оригинале, а текст без \fs (созданный в этой версии) остаётся "по умолчанию" (Sz = 0).

            // RTF всегда заканчивает последний абзац \par - убираем этот лишний перевод строки
            if (text.Length > 0 && text[text.Length - 1] == '\n')
            {
                text.Length--;
                fmts.RemoveAt(fmts.Count - 1);
            }

            return (text.ToString(), fmts.ToArray());
        }

        /// <summary>Записывает текст и форматирование в RTF (только ASCII, остальное - \uN?).</summary>
        public static string Write(string text, IReadOnlyList<Fmt> fmts)
        {
            var sb = new StringBuilder();
            sb.Append(@"{\rtf1\ansi\ansicpg1252\uc1\deff0{\fonttbl{\f0\fnil Segoe UI;}}\pard\plain\f0 ");

            int i = 0;
            while (i < text.Length)
            {
                Fmt f = i < fmts.Count ? fmts[i] : default;
                int j = i + 1;
                while (j < text.Length && (j < fmts.Count ? fmts[j] : default).Equals(f)) j++;

                if (f.IsDefault)
                {
                    AppendEscaped(sb, text, i, j);
                }
                else
                {
                    sb.Append('{');
                    if (f.B) sb.Append(@"\b");
                    if (f.I) sb.Append(@"\i");
                    if (f.U) sb.Append(@"\ul");
                    if (f.S) sb.Append(@"\strike");
                    if (f.Sz > 0)
                        sb.Append(@"\fs").Append(((int)Math.Round(f.Sz * 1.5)).ToString(CultureInfo.InvariantCulture));
                    sb.Append(' ');
                    AppendEscaped(sb, text, i, j);
                    sb.Append('}');
                }
                i = j;
            }

            sb.Append(@"\par}");
            return sb.ToString();
        }

        private static void AppendEscaped(StringBuilder sb, string text, int start, int end)
        {
            for (int k = start; k < end; k++)
            {
                char ch = text[k];
                switch (ch)
                {
                    case '\\':
                        sb.Append(@"\\");
                        break;
                    case '{':
                        sb.Append(@"\{");
                        break;
                    case '}':
                        sb.Append(@"\}");
                        break;
                    case '\n':
                        sb.Append("\\par\r\n");
                        break;
                    case '\t':
                        sb.Append("\\tab ");
                        break;
                    case '\r':
                        break;
                    default:
                        if (ch >= 32 && ch < 127)
                            sb.Append(ch);
                        else if (ch >= 127)
                            sb.Append(@"\u")
                              .Append(((int)unchecked((short)ch)).ToString(CultureInfo.InvariantCulture))
                              .Append('?');
                        break;
                }
            }
        }
    }

    /// <summary>Применяет Fmt к отображаемым фрагментам строк AvaloniaEdit.</summary>
    internal sealed class FormatColorizer : DocumentColorizingTransformer
    {
        private readonly DiaryFormatter _owner;

        public FormatColorizer(DiaryFormatter owner)
        {
            _owner = owner;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int end = line.EndOffset;
            int i = line.Offset;
            while (i < end)
            {
                Fmt f = _owner.FormatAt(i);
                int j = i + 1;
                while (j < end && _owner.FormatAt(j).Equals(f)) j++;

                if (!f.IsDefault)
                {
                    Fmt applied = f;
                    ChangeLinePart(i, j, element => Apply(element, applied));
                }
                i = j;
            }
        }

        private static void Apply(VisualLineElement element, Fmt f)
        {
            var props = element.TextRunProperties;

            if (f.B || f.I)
            {
                var tf = props.Typeface;
                props.SetTypeface(new Typeface(
                    tf.FontFamily,
                    f.I ? FontStyle.Italic : tf.Style,
                    f.B ? FontWeight.Bold : tf.Weight,
                    tf.Stretch));
            }

            if (f.Sz > 0)
                props.SetFontRenderingEmSize(f.Sz);

            if (f.U || f.S)
            {
                var decorations = new TextDecorationCollection();
                if (f.U) decorations.Add(new TextDecoration { Location = TextDecorationLocation.Underline });
                if (f.S) decorations.Add(new TextDecoration { Location = TextDecorationLocation.Strikethrough });
                props.SetTextDecorations(decorations);
            }
        }
    }

    /// <summary>
    /// Хранит форматирование символов для TextEditor и держит его в синхроне с документом.
    /// Поведение как у RichTextBox: форматирование при вводе берётся от символа перед кареткой,
    /// переключатель без выделения действует на следующий набранный текст.
    /// Отмена/возврат (Undo/Redo) откатывают текст, но не форматирование.
    /// </summary>
    public sealed class DiaryFormatter
    {
        private readonly TextEditor _ed;
        private readonly List<Fmt> _fmts = new();
        private Fmt _typing;
        private bool _loading;

        /// <summary>Форматирование изменилось без изменения текста - заметку нужно сохранить.</summary>
        public event Action? Modified;

        public bool IsLoading => _loading;

        public DiaryFormatter(TextEditor editor)
        {
            _ed = editor;
            _ed.Document ??= new TextDocument();
            _ed.Document.Changed += OnDocumentChanged;
            _ed.TextArea.TextView.LineTransformers.Add(new FormatColorizer(this));
            _ed.TextArea.Caret.PositionChanged += (_, _) => UpdateTypingFromCaret();
        }

        public Fmt FormatAt(int offset) => offset >= 0 && offset < _fmts.Count ? _fmts[offset] : default;

        /// <summary>Размер шрифта у каретки / начала выделения (для комбобокса).</summary>
        public double CurrentSize
        {
            get
            {
                Fmt f = _ed.SelectionLength > 0 ? FormatAt(_ed.SelectionStart) : _typing;
                return f.Sz > 0 ? f.Sz : _ed.FontSize;
            }
        }

        private void OnDocumentChanged(object? sender, DocumentChangeEventArgs e)
        {
            if (e.RemovalLength > 0 && e.Offset <= _fmts.Count)
            {
                int remove = Math.Min(e.RemovalLength, _fmts.Count - e.Offset);
                if (remove > 0) _fmts.RemoveRange(e.Offset, remove);
            }

            if (e.InsertionLength > 0)
            {
                Fmt f = _loading ? default : _typing;
                _fmts.InsertRange(Math.Min(e.Offset, _fmts.Count), Enumerable.Repeat(f, e.InsertionLength));
            }

            // страховка: длина списка всегда равна длине документа
            int length = _ed.Document.TextLength;
            while (_fmts.Count < length) _fmts.Add(default);
            if (_fmts.Count > length) _fmts.RemoveRange(length, _fmts.Count - length);
        }

        private void UpdateTypingFromCaret()
        {
            if (_loading) return;
            int offset = _ed.CaretOffset;
            int index = offset > 0 ? offset - 1 : 0;
            _typing = FormatAt(index);
        }

        /// <summary>Заменяет содержимое редактора (загрузка заметки). Автосохранение при этом не должно срабатывать.</summary>
        public void SetContent(string text, IReadOnlyList<Fmt> formats)
        {
            _loading = true;
            try
            {
                _ed.Text = text;
                _fmts.Clear();
                for (int k = 0; k < text.Length; k++)
                    _fmts.Add(k < formats.Count ? formats[k] : default);
                _ed.Document.UndoStack.ClearAll();
                _typing = default;
                _ed.TextArea.TextView.Redraw();
            }
            finally
            {
                _loading = false;
            }
        }

        public string ToRtf() => RtfIO.Write(_ed.Text ?? "", _fmts);

        public void ToggleBold() => Toggle(f => f.B, (f, v) => f with { B = v });
        public void ToggleItalic() => Toggle(f => f.I, (f, v) => f with { I = v });
        public void ToggleUnderline() => Toggle(f => f.U, (f, v) => f with { U = v });
        public void ToggleStrike() => Toggle(f => f.S, (f, v) => f with { S = v });

        public void ApplySize(double size)
        {
            double stored = Math.Abs(size - _ed.FontSize) < 0.01 ? 0 : size;
            int start = _ed.SelectionStart;
            int length = _ed.SelectionLength;
            if (length == 0)
            {
                _typing = _typing with { Sz = stored };
            }
            else
            {
                int end = Math.Min(start + length, _fmts.Count);
                for (int k = start; k < end; k++) _fmts[k] = _fmts[k] with { Sz = stored };
                AfterFormatEdit();
            }
            _ed.Focus();
        }

        public void Undo()
        {
            var stack = _ed.Document.UndoStack;
            if (stack.CanUndo) stack.Undo();
            _ed.Focus();
        }

        public void Redo()
        {
            var stack = _ed.Document.UndoStack;
            if (stack.CanRedo) stack.Redo();
            _ed.Focus();
        }

        private void Toggle(Func<Fmt, bool> get, Func<Fmt, bool, Fmt> set)
        {
            int start = _ed.SelectionStart;
            int length = _ed.SelectionLength;

            if (length == 0)
            {
                _typing = set(_typing, !get(_typing));
                _ed.Focus();
                return;
            }

            int end = Math.Min(start + length, _fmts.Count);
            bool allOn = true;
            for (int k = start; k < end; k++)
            {
                if (!get(_fmts[k]))
                {
                    allOn = false;
                    break;
                }
            }
            for (int k = start; k < end; k++) _fmts[k] = set(_fmts[k], !allOn);

            AfterFormatEdit();
            _ed.Focus();
        }

        private void AfterFormatEdit()
        {
            _ed.TextArea.TextView.Redraw();
            Modified?.Invoke();
        }
    }
}
