# Snaptrude Manager

- Sanptrude Manager is used as Revit<>Snaptrude bi-directional link.
- Allowing users to easily import and export models between Revit and Snaptrude.
- This enables better design changes, client presentations, and sign offs to be completed through Snaptrude's cloud-based collaborative platform.
- This ensures an efficient design workflow, with instant feedback and ease of use.

## Manager & Repo Setup:

1. Install Visual Studio.
2. Clone SnaptrudeManager repo.
3. Download and install the latest Snaptrude manager. Download it from either:
   - [Snaptrude's drive](https://drive.google.com/drive/folders/1ToMcqBVUU8VR0U5uW1VEiL1CJXcp102J?usp=drive_link)
   - Snaptrude > open any project > import > Revit Files > Download Snaptrude Manager
4. Install required version of Revit.
5. Install latest version of [RevitAddinManager](https://github.com/chuongmep/RevitAddInManager/releases)
   - this helps to reflect changes in SnaptrudeManager plugin without needing to reopen Revit
6. Run Revit and check if the Snaptrude manager add-in is installed.

## To use Snaptrude-manager through Visual Studio (development) follow the below steps:

1. Checkout relevant branch.
2. Download [IFC.zip](https://drive.google.com/file/d/1IP67UnEYS3VAbzbpW4GEkl3b-Atf9dAL/view?usp=sharing) file. Extract and put the IFC folder in `<repo-path>/revit-addin/SnaptrudeManagerAddin/lib`
   - Used to get dlls for IFC export
   - REQUIRED to compile Revit Plugin in Visual Studio
3. Open Visual Studio, open SnaptrudeManager.sln, Build > Build Solution
4. Open the following file:
   `programData\autodesk\Revit\Addins\<REVIT_VERSION>\snaptrudeManagerAddin.addin`
   and replace the Assembly config in it with below value:
   ```
   <Assembly>{repo-path}\revit-addin\SnaptrudeManagerAddin\bin\Debug\{REVIT_VERSION}\SnaptrudeManagerAddin.dll</Assembly>
   ```
   - this will use the SnaptrudeManager plugin built from the local repo, instead of the one that we just installed from setup.
5. Use the debug profile corresponding to the desired Revit version from the toolbar

> Note:
>
> 1. Make sure electron-app react server (`npm run start`) is running
> 2. Update URLs in `{users}programData\snaptrude-manager\urls.json` to use different snaptrudereact/snaptrudestaging servers (e.g local or PR testing)

## To use Snaptrude-manager through Visual Studio (development) and Add-in Manager follow the below steps:

1. Checkout relevant branch.
2. Select build configuration to the desired Revit Version
3. Build SnaptrudeManagerAddin.csproj
4. Open Revit project
5. Open Add-in Manager on Revit 'Addins' tab -> 'Add-in Manager (Manual Mode)'
6. Click on 'Load' button and serach for the latest built dll
7. Double click on 'SnaptrudeManagerAddin.Command' to direclty run the reconcile or double click on 'SnaptrudeManagerAddin.DynamoExport' to run the electron app

## To build the Snaptrude-manager:

To be done for building and deploying the final plugin installer.

1. One time setup
   - Install [inno-setup](https://jrsoftware.org/isinfo.php).
   - Create `installers` folder inside `build-installer` put [these](https://drive.google.com/drive/folders/1rvZJ7jytefcPT2KEGaHOcRnVh6wW_vfY) installers inside it.
2. If there is any new change in the `electron-app` under your branch then build the electron app and replace `snaptrude-manager-<version>` Setup inside `build-installer`.
3. On feature branches:
   - For building branch name should not have "/" in it. We can't have branch name like "feature/export", instead we can use "feature-export".
   - Checkout the branch and build it from visual studio.
   - Run `build.ps1` file to create the installer for the new Snaptrude manager.
   - Go inside `<root-directory>/build-installer/out` and check new files' version in the format `<branch-name>_<YYYYMMDD>`.
4. On master and dev:
   - The version is read from version.txt file. Update the file with correct version
   - run build.ps1.
   - Check inside `<root-directory>/build-installer/out` for the build with correct version names.

## Unit Tests for revit-addin

Check out this notion doc for setup, running and debugging Unit Tests for revit-addin [Unit Tests Doc](https://www.notion.so/snaptrude/Unit-Testing-Revit-Plugin-2316ff48f78441bbace47539aabc73d1)

## Documentation with Doxygen

- Documentation can be viewed from the [official website](https://www.doxygen.nl/manual/docblocks.html).
- `JAVADOC_AUTOBRIEF` is enabled, which means a brief description ends at the first dot followed by a space or new line.

### Doxygen Setup

1. Download the latest installer setup from Doxygen [github](https://github.com/doxygen/doxygen/releases) or [website](https://www.doxygen.nl/download.html).
2. Open the installer, remember to check on the doxywizard GUI, and continue the installation process.
3. Download the [DoxyFile from Drive](https://drive.google.com/file/d/1z-wlGA-IwhTYAT9aONaP_OcpTmHUYJMx/view?usp=sharing). This defines the settings to be used by the documentation system.
4. (optional) download [doxygen-awesome.css](https://github.com/jothepro/doxygen-awesome-css/blob/main/doxygen-awesome.css) for a better UI.

### Generating documentation

> Video of below steps for generating documentation: [link](https://drive.google.com/file/d/1RIRzjhZia4LlGmkYadiBiTp80Xjmh47Y/view?usp=sharing)

1. Open doxywizard program.
2. Go to File > Open > select the downloaded DoxyFile.
3. We will set the following parameters manually via UI:

   ```
   <working directory>
   INPUT
   OUTPUT_DIRECTORY
   (optional) HTML_EXTRA_STYLESHEET
   ```

   - `working directory`: It will use it as a scratchpad for writing temporary files. browse and set it as any directory you want.
   - `INPUT`: In the Wizard tab, for 'Source Code Directory', browse and select `<root-directory>/revit-addin/TrudeSerializer/Importer`
   - `OUTPUT_DIRECTORY`: In the Wizard tab, for 'Destination Directory', browse and select the directory you want to save the generated HTML documentation. Note that this documentation _should never_ be pushed.
   - (optional) `HTML_EXTRA_STYLESHEET`: In the Expert tab, choose HTML topic, scroll down to find HTML_EXTRA_STYLESHEET, and browse to `doxygen-awesome.css` file.

4. You can save this settings configuration by File > Save.
5. Go to Run tab, then Run Doxygen. This should generate the HTML documentation. Click on Show HTML Output to view it.
6. If you're adding/modifiying the documentation, scroll the Output section for any errors after doxywizard finishes generating the doc.

## Debugging

1. If you've added (or pulled) a new directory or files and intellij is unable to identify them, then close Visual Studio, delete `.vs` folder and reopen Visual Studio
2. Double check the path for `SnaptrudeManagerAddin.dll` in `SnaptrudeManagerAddin.addin`
3. For log-in issues in electron-app, log-out and log-in in snaptrudereact, then try again.
4. Make sure the url in `urls.json` is correct - e.g. URL doesn't end with `/`
