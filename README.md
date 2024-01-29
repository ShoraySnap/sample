# Snaptrude Manager

- Sanptrude Manager is used as Revit<>Snaptrude bi-directional link.
- Allowing users to easily import and export models between Revit and Snaptrude.
- This enables better design changes, client presentations, and sign offs to be completed through Snaptrude’s cloud-based collaborative platform.
- This ensures an efficient design workflow, with instant feedback and ease of use.

## Manager & Repo Setup:

1. Install Visual Studio.
2. Clone SnaptrudeManager repo.
3. Add below `.dll` files inside folder `<root-directory>/revit-addin/SnaptrudeManagerAddin`
    - [DynamoCore.dll](https://drive.google.com/file/d/1MXEI2x1jRIaxpha0q0SKobZvtjmEY5AD/view?usp=drive_link)
    - [DynamoRevitDS.dll](https://drive.google.com/file/d/1wEVZMO3NSJndkoVB0hdcnAii8SsF5kGQ/view?usp=drive_link)
4. Download and install the latest Snaptrude manager.
5. Install Required version of Revit.
6. Run Revit and check if the Snaptrude manager add-in is installed.

## To use Snaptrude-manager through Visual Studio (development) follow the below steps:
1. Checkout relevant branch.
2. Build SnaptrudeManager.sln
3. Open below file:
`{users}\AppData\Roaming\Autodesk\Revit\Addins\{REVIT_VERSION}\SnaptrudeManagerAddin.addin`
4. Replace the Assembly config with below value:
    ```
    <Assembly>{repo-path}\revit-addin\SnaptrudeManagerAddin\bin\Debug\SnaptrudeManagerAddin.dll</Assembly>
    ```

## To build the Snaptrude-manager:
1. One time setup
    - Install [inno-setup](https://jrsoftware.org/isinfo.php).
    - Create `custom_families` folder inside `<root-directory>/build-installer/misc`
    - Create `installers` folder inside `build-installer` put [these](https://drive.google.com/drive/folders/1rvZJ7jytefcPT2KEGaHOcRnVh6wW_vfY) installers inside it.
2. Set the dynamo script version to be used in the `dynamo_script_version.txt` file. (Do not change it if there is no change for dynamo scripts.)
3. If there is any new change in the `electron-app` under your branch then build the electron app and replace `snaptrude-manager-<version>` Setup insdie `build-installer`.
4. On feature branches:
    - Checkout the branch and build it from visual studio.
    - Run `build.ps1` file to create the installer for the new Snaptrude manager.
    - Go inside `<root-directory>/build-installer/out` and check new files' version in the format `<branch-name>_<YYYYMMDD>`.
5. On master and dev:
    - Checkout the branch and build it from visual studio.
    - The version is read from version.txt file. Update the file with corretct version  and run build.ps1.
    - Check inside `<root-directory>/build-installer/out` for the build with correct version names.
