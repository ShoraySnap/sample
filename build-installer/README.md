# How to build

- In the update script, be sure to check which lines are uncommented
- Verify the installers being bundled
- Place the installers on the drive

## On feature branches

Run `build.ps1`, this will build the output files with the version in the format `<branch-name>_<YYYYMMDD>`.

## On `master` and `dev`

The version is read from the `version.txt` file. Update the version in the file appropriatly and 
run `build.ps1`.
