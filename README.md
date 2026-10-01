# Nanosoft TextProcessor

WPF-приложение для потоковой обработки текстовых файлов согласно `SPECIFICATION.md`.

## Реализовано

- .NET 8 / WPF / MVVM.
- Библиотека `TextProcessor.Lib` с расширяемой стратегией обработки.
- Удаление слов короче заданной длины.
- Три режима обработки пунктуации.
- Настройка цифр, дефисов, апострофов и дополнительных символов слова.
- Нормализация пробелов и сохранение переносов строк.
- UTF-8, UTF-16 LE/BE, Windows-1251, ASCII и определение BOM.
- Потоковая обработка через `StreamReader`/`StreamWriter` с ограниченным буфером.
- Отмена через `CancellationToken`.
- Пакетная обработка с ограничением параллелизма.
- Автоматическое формирование выходных имён.
- Безопасная обработка входного и выходного файла с одинаковым путём через временный файл и атомарную замену.
- Модульные тесты для стратегии, шаблонов имён, файлового и пакетного процессоров.

## Сборка

Требуется Windows и .NET 8 SDK.

```text
dotnet restore TextProcessor.sln
dotnet build TextProcessor.sln --configuration Release
dotnet test TextProcessor.sln --configuration Release
```

Запуск:

```text
dotnet run --project src/TextProcessor.UI/TextProcessor.UI.csproj
```

Подробные требования находятся в [SPECIFICATION.md](SPECIFICATION.md).
