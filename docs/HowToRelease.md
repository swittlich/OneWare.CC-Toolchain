# A Release in 10 Steps
1. **Create a release branch from `develop`**
2. **Update `Release.md`** and add release notes
3. **Update `oneware-extension.json`** and add a new target version
4. **Set the version in `OneWare.CologneChip.csproj`** to the release version
5. **Commit and push**
6. **Create a merge request (Pull Request) on GitHub** against the `main` branch
7. **Review and merge** the merge request
8. **Publish the release** through GitHub Actions
9. **Merge `main` into `develop`** to synchronize the branches
10. **Set the version in `OneWare.CologneChip.csproj`** in `develop` to `<New Release Version>-SNAPSHOT`

