#!/usr/bin/env python3
"""Собирает портативный архив релиза из каталога stage/.

Внутри архива (всё лежит в корне, без вложенной папки - так его умеет распаковывать и Updater, и обычный
распаковщик в любую папку):
    DNDHelper/            приложение
    Updater/              DNDUpdater (самообновление)
    portable.txt          маркер портативного режима
    README-PORTABLE.txt

ZipFile.ExtractToDirectory в .NET не всегда сохраняет бит исполняемости, поэтому Updater сам выставляет его на
Linux/macOS; здесь же прописываем 0755 для запускаемых файлов, чтобы архив правильно распаковывался и обычным unzip.
"""
import argparse
import os
import stat
import sys
import time
import zipfile

EXECUTABLES = {"DNDHelper/DNDHelper", "Updater/DNDUpdater"}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--stage", required=True)
    ap.add_argument("--out", required=True)
    args = ap.parse_args()

    stage = os.path.abspath(args.stage)
    for required in ("DNDHelper", "Updater", "portable.txt"):
        if not os.path.exists(os.path.join(stage, required)):
            print(f"В {stage} нет {required}", file=sys.stderr)
            return 1

    os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
    count = 0
    with zipfile.ZipFile(args.out, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for root, dirs, files in os.walk(stage):
            dirs.sort()
            for name in sorted(files):
                full = os.path.join(root, name)
                arc = os.path.relpath(full, stage).replace(os.sep, "/")
                info = zipfile.ZipInfo(arc, time.localtime(os.path.getmtime(full))[:6])
                info.compress_type = zipfile.ZIP_DEFLATED
                mode = 0o755 if arc in EXECUTABLES else 0o644
                info.external_attr = (stat.S_IFREG | mode) << 16
                with open(full, "rb") as f:
                    z.writestr(info, f.read())
                count += 1
    print(f"{args.out}: {count} файлов, {os.path.getsize(args.out) / 1024 / 1024:.1f} МБ")
    return 0


if __name__ == "__main__":
    sys.exit(main())
