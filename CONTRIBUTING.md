# Contributing

The project is small, opinionated, and maintained by one person, so the best way to help is almost always to open an issue. That keeps the conversation in one place and makes it easy to land the fix in a way that fits the rest of the library.

## Open an issue first

The fastest path to a fix or a new feature is a [GitHub issue](https://github.com/destbg/SQLite.Framework/issues). A short example that shows the LINQ query, the generated SQL or the exception, and what you expected is usually enough.

- **Bug reports.** Include the .NET version, the provider package you use, and a small repro if you can.
- **Missing SQLite features.** If SQLite supports it and the framework does not, please ask. These get added quickly in most cases.
- **API or behavior questions.** Issues are fine for these too. There is no separate discussions board.

If you are not sure whether something is a bug or expected behavior, open an issue anyway. It is easier to answer a question than to guess what someone ran into.

## About pull requests

Pull requests are welcome for clear, small fixes such as typos and doc tweaks. For anything larger, please open an issue first so we can agree on the shape of the change.

## What I will likely say no to

- Large refactors without a prior issue.
- New runtime dependencies.
- Features that pull rows into memory to make a LINQ method work. The library throws a clear exception instead, and that is on purpose.
- Support for databases other than SQLite.

None of this is meant to be unwelcoming. The scope of the project is narrow on purpose, and saying no to some things is what keeps it small and fast.

## Code of conduct

Be kind. Assume the other person is acting in good faith. That is the whole rule.

## License

By contributing, you agree that your contribution is licensed under the MIT license, the same as the rest of the project.
