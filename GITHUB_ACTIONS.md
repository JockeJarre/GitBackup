# GitHub Actions CI/CD Setup for GitBackup

This document explains the GitHub Actions workflow setup for the GitBackup project.

## Workflow Overview

The `.github/workflows/build.yml` file contains a complete CI/CD pipeline that:

1. **Triggers on**:
   - Push to `main`, `master`, or `develop` branches
   - Pull requests to `main` or `master` branches

2. **Build Process**:
   - Uses Ubuntu latest runner
   - Sets up .NET 8 SDK
   - Calculates build number using git commit count
   - Restores dependencies and builds in Release configuration
   - Runs tests (if available)
   - Publishes for Windows x64 and Linux x64

3. **Versioning Strategy**:
   - **Base version**: `1.0.0` (configurable)
   - **Build number**: Total git commit count (`git rev-list --count HEAD`)
   - **Final version**: `1.0.0.{build_number}`
   - **Assembly versions**: Set to match the final version

4. **Artifacts**:
   - Windows x64 executable and dependencies
   - Linux x64 executable and dependencies
   - Version information file

5. **Releases** (only on main/master):
   - Creates GitHub release with version tag
   - Uploads zip (Windows) and tar.gz (Linux) packages
   - Includes release notes with commit information

## Version Management

### Assembly Version Properties
The project uses these MSBuild properties for version management:

- `VersionPrefix`: Base version (default: 1.0.0)
- `VersionSuffix`: Pre-release suffix (empty for release builds)
- `BuildNumber`: Incremental build number from git commits
- `AssemblyVersion`: `{VersionPrefix}.{BuildNumber}`
- `FileVersion`: `{VersionPrefix}.{BuildNumber}`
- `InformationalVersion`: Full version with metadata

### Directory.Build.props
Contains common version properties shared across the solution:

```xml
<VersionPrefix Condition="'$(VersionPrefix)' == ''">1.0.0</VersionPrefix>
<BuildNumber Condition="'$(BuildNumber)' == ''">0</BuildNumber>
<AssemblyVersion>$(VersionPrefix).$(BuildNumber)</AssemblyVersion>
```

## Local Testing

### Build Test Script
Use `build-test.ps1` to simulate the CI build process locally:

```powershell
# Default build
.\build-test.ps1

# Specific build number
.\build-test.ps1 -BuildNumber 123

# Pre-release version
.\build-test.ps1 -BuildNumber 123 -VersionSuffix "beta"
```

### Manual Build Commands
To build with specific version information:

```bash
# Release build with version
dotnet build --configuration Release \
  -p:VersionPrefix=1.0.0 \
  -p:BuildNumber=42 \
  -p:AssemblyVersion=1.0.0.42 \
  -p:FileVersion=1.0.0.42

# Publish for Windows
dotnet publish --configuration Release \
  --runtime win-x64 \
  --self-contained false \
  -p:VersionPrefix=1.0.0 \
  -p:BuildNumber=42
```

## Customization

### Change Base Version
To change the base version from 1.0.0:

1. Update the workflow file: `VersionPrefix=1.1.0`
2. Update `Directory.Build.props`: `<VersionPrefix>1.1.0</VersionPrefix>`

### Add More Platforms
To support additional platforms, add more publish steps:

```yaml
- name: Publish macOS x64
  run: |
    dotnet publish --configuration Release --runtime osx-x64 --self-contained false \
      -p:VersionPrefix=1.0.0 \
      -p:BuildNumber=${{ steps.build_number.outputs.BUILD_NUMBER }} \
      --output ./publish/osx-x64/
```

### Custom Build Number
To use a different build numbering strategy, modify the `Get build number` step:

```yaml
- name: Get build number
  id: build_number
  run: |
    # Use GitHub run number instead of commit count
    BUILD_NUMBER=${{ github.run_number }}
    echo "BUILD_NUMBER=$BUILD_NUMBER" >> $GITHUB_OUTPUT
```

## Troubleshooting

### Version Not Updating
- Check that MSBuild properties are correctly passed
- Verify `Directory.Build.props` is in the project root
- Ensure build commands include version parameters

### Release Not Created
- Verify the push is to `main` or `master` branch
- Check GitHub token permissions for releases
- Ensure artifacts are properly uploaded before release step

### Build Failures
- Check .NET SDK version compatibility
- Verify all required packages are restored
- Check for compilation errors in the logs

## Security Considerations

- The workflow uses `GITHUB_TOKEN` which has limited permissions
- No secrets are required for basic build and release
- Artifacts are publicly available in releases
- Consider adding code signing for production releases
