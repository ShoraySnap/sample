using SnaptrudeManagerUI.Commands;
using SnaptrudeManagerUI.Models;
using SnaptrudeManagerUI.Services;
using SnaptrudeManagerUI.API;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using NLog;

namespace SnaptrudeManagerUI.ViewModels
{
    public class SelectFolderViewModel : ViewModelBase
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public ObservableCollection<FolderViewModel> CurrentPathFolders { get; private set; }
        public ObservableCollection<FolderViewModel> Breadcrumb { get; private set; }
        public ICommand OpenFolderCommand { get; private set; }
        public ICommand NavigateToFolderCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand BeginExportCommand { get; private set; }

        private string team_id = "";
        private string folder_id = "";

        private bool isLoaderVisible = true;
        public bool IsLoaderVisible
        {
            get { return isLoaderVisible; }
            set { isLoaderVisible = value; OnPropertyChanged("IsLoaderVisible"); OnPropertyChanged("ExportIsEnable"); }
        }

        private bool showErrorMessage = false;
        public bool ShowErrorMessage
        {
            get { return showErrorMessage; }
            set { showErrorMessage = value; OnPropertyChanged("ShowErrorMessage"); }
        }

        private bool addBreadcrumbs = true;

        private bool isWorkspaceSelected;

        public bool IsWorkspaceSelected
        {
            get { return isWorkspaceSelected; }
            set { isWorkspaceSelected = value; OnPropertyChanged("IsWorkspaceSelected"); OnPropertyChanged("ExportIsEnable"); }
        }

        public bool ExportIsEnable => ViewIs3D && IsWorkspaceSelected && !IsLoaderVisible && !ShowErrorMessage;

        public bool ViewIs3D => MainWindowViewModel.Instance.IsActiveView3D;
        public bool ViewIsNot3D => !ViewIs3D;
        private FolderViewModel AllWorkspacesViewModel = new FolderViewModel(new Folder("-1", "All Workspaces", Constants.WorkspaceType.Top, "-1"));

        private void MainWindowViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainWindowViewModel.Instance.IsActiveView3D))
            {
                OnPropertyChanged(nameof(ViewIs3D));
                OnPropertyChanged(nameof(ViewIsNot3D));
                OnPropertyChanged(nameof(ExportIsEnable));
            }
        }
        public SelectFolderViewModel(NavigationService backNavigationService, NavigationService exportNavigationService)
        {
            TransformCommand transformMainWindowViewModelCommand = new TransformCommand(
                new TransformService(MainWindowViewModel.Instance, (viewmodel) =>
                {
                    ((MainWindowViewModel)viewmodel).PropertyChanged += MainWindowViewModel_PropertyChanged;
                    return viewmodel;
                }));
            transformMainWindowViewModelCommand.Execute(new object());
            BeginExportCommand = new RelayCommand((o) => { BeginExport(o, exportNavigationService); });
            BackCommand = new NavigateCommand(backNavigationService);
            CurrentPathFolders = new ObservableCollection<FolderViewModel>();
            Breadcrumb = new ObservableCollection<FolderViewModel>();
            OpenFolderCommand = new RelayCommand(GetSubFoldersAsync);
            NavigateToFolderCommand = new RelayCommand(NavigateToFolder);
            // Initialize with root folders
            LoadRootFolders();
        }

        private void BeginExport(object param, NavigationService exportNavigationService) 
        {
            Store.Set("team_id", team_id);
            Store.Set("folder_id", folder_id);
            Store.Save();

            var navCmd = new NavigateCommand(exportNavigationService);
            navCmd.Execute(param);
        }

        private async void LoadRootFolders()
        {
            Breadcrumb.Clear();
            GetSubFoldersAsync(AllWorkspacesViewModel);
        }

        private async void GetSubFoldersAsync(object parameter)
        {
            try
            {
                if (parameter is FolderViewModel parentFolder)
                {
                    IsLoaderVisible = true;

                    List<Folder> subFolders = new List<Folder>();
                    var subFolderViewModels = new List<FolderViewModel>();
                    if (parentFolder.FolderType == Constants.WorkspaceType.Top)
                    {
                        subFolders = await SnaptrudeRepo.GetUserWorkspacesAsync();
                        subFolderViewModels = new List<FolderViewModel>();
                        foreach (var subFolder in subFolders)
                        {
                            subFolderViewModels.Add(new FolderViewModel(subFolder));
                        }
                    }
                    else
                    {
                        CurrentPathFolders.Clear();
                        subFolders = await SnaptrudeRepo.GetSubFoldersAsync(parentFolder);
                        subFolderViewModels = new List<FolderViewModel>();
                        foreach (var subFolder in subFolders)
                        {
                            subFolderViewModels.Add(new FolderViewModel(subFolder));
                        }
                        team_id = parentFolder.TeamId;
                        folder_id = parentFolder.FolderType == Constants.WorkspaceType.Folder ? parentFolder.Id : "root";
                    }
                    PopulateSubFolders(subFolderViewModels);
                    if (addBreadcrumbs == true)
                    {
                        parentFolder.Name = parentFolder.Name.Replace("_", "__"); //https://www.charlespetzold.com/blog/2006/01/061004.html
                        Breadcrumb.Add(parentFolder);
                        setExportButton();
                    }
                    addBreadcrumbs = true;
                    IsLoaderVisible = false;
                }
            }
            catch (Exception ex)
            {
                if (parameter is FolderViewModel parentFolder)
                {
                    logger.Error(parentFolder.Id + " " + parentFolder.Name + " " + parentFolder.FolderType.ToString() + " " + parentFolder.TeamId);
                }
                logger.Error(ex.Message);
                ShowErrorMessage = true;
                IsLoaderVisible = false;
            }
        }

        private void PopulateSubFolders(List<FolderViewModel> subFolders)
        {
            foreach (var subFolder in subFolders)
            {
                CurrentPathFolders.Add(subFolder);
            }
        }


        private void NavigateToFolder(object parameter)
        {
            if (parameter is FolderViewModel folder)
            {
                // Update the list of current path folders
                CurrentPathFolders.Clear();
                UpdateBreadcrumb(folder);
                addBreadcrumbs = false;
                GetSubFoldersAsync(folder);
                // Update the breadcrumb
            }
            setExportButton();
        }

        private void setExportButton()
        {
            IsWorkspaceSelected = Breadcrumb.Count > 1;
        }

        private void UpdateBreadcrumb(FolderViewModel folder)
        {
            for (int i = Breadcrumb.Count - 1; i >= 0; i--)
            {
                var topBreadcrumb = Breadcrumb[i];
                if (topBreadcrumb.Id == folder.Id && topBreadcrumb.FolderType == folder.FolderType)
                {
                    break;
                }
                Breadcrumb.RemoveAt(i);
            }
        }
    }
}
