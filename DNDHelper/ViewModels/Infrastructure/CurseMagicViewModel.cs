namespace DNDHelper.ViewModels.Infrastructure
{
    // Заглушка. Логика сейчас в Modules.MagicSpells.MagicCurse (статический класс,
    // принимает CurseMagic-окно параметром) — при переносе в MVVM это, скорее всего,
    // разъедется на несколько свойств/команд (ColdBoosting/GiftOfHealing/GiftOfDark
    // выглядят как самостоятельные под-разделы формы).
    public partial class CurseMagicViewModel : ViewModelBase
    {
    }
}
