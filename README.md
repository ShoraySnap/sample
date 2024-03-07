# Snaptrude Manager

- Sanptrude Manager is used as Revit<>Snaptrude bi-directional link.
- Allowing users to easily import and export models between Revit and Snaptrude.
- This enables better design changes, client presentations, and sign offs to be completed through Snaptrude's cloud-based collaborative platform.
- This ensures an efficient design workflow, with instant feedback and ease of use.

## Manager & Repo Setup:

1. Install Visual Studio.
2. Clone SnaptrudeManager repo.
4. Download and install the latest Snaptrude manager.
5. Install Required version of Revit.
6. Run Revit and check if the Snaptrude manager add-in is installed.

## To use Snaptrude-manager through Visual Studio (development) follow the below steps:
1. Checkout relevant branch.
2. Download [IFC.zip](https://drive.google.com/file/d/1IP67UnEYS3VAbzbpW4GEkl3b-Atf9dAL/view?usp=sharing) file. Extract and put the IFC folder in `<root-directory>/revit-addin/SnaptrudeManagerAddin/lib` (REQUIRED to compile Revit Plugin in Visual Studio)
3. Build SnaptrudeManager.sln
4. Open below file:
`{users}\AppData\Roaming\Autodesk\Revit\Addins\{REVIT_VERSION}\SnaptrudeManagerAddin.addin`
5. Replace the Assembly config with below value:
    ```
    <Assembly>{repo-path}\revit-addin\SnaptrudeManagerAddin\bin\Debug\SnaptrudeManagerAddin.dll</Assembly>
    ```

## To build the Snaptrude-manager:
1. One time setup
    - Install [inno-setup](https://jrsoftware.org/isinfo.php).
    - Create `custom_families` folder inside `<root-directory>/build-installer/misc`
    - Create `installers` folder inside `build-installer` put [these](https://drive.google.com/drive/folders/1rvZJ7jytefcPT2KEGaHOcRnVh6wW_vfY) installers inside it.
2. If there is any new change in the `electron-app` under your branch then build the electron app and replace `snaptrude-manager-<version>` Setup inside `build-installer`.
3. On feature branches:
    - For building branch name should not have "/" in it. We can't have branch name like "feature/export", instead we can use "feature-export".
    - Checkout the branch and build it from visual studio.
    - Run `build.ps1` file to create the installer for the new Snaptrude manager.
    - Go inside `<root-directory>/build-installer/out` and check new files' version in the format `<branch-name>_<YYYYMMDD>`.
4. On master and dev:
    - Checkout the branch and build it from visual studio.
    - The version is read from version.txt file. Update the file with corretct version  and run build.ps1.
    - Check inside `<root-directory>/build-installer/out` for the build with correct version names.
