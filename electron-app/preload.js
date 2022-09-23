const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electronAPI', {
    // react to electron
    openPageInDefaultBrowser: (url) => ipcRenderer.send('openPageInDefaultBrowser', [url]),
    flushUserData: () => ipcRenderer.send('flushUserData'),
    updateUserData: (data) => ipcRenderer.send('updateUserData', [data]),
    uploadToSnaptrude: () => ipcRenderer.send('uploadToSnaptrude'),
    importFromSnaptrude: () => ipcRenderer.send('importFromSnaptrude'),
    log: (messages) => ipcRenderer.send('log', [messages]),
    operationSucceeded: () => ipcRenderer.send('operationSucceeded'),
    operationFailed: () => ipcRenderer.send('operationFailed'),
    openDevtools: () => ipcRenderer.send('openDevtools'),
    showLogs: () => ipcRenderer.send('showLogs'),
    
    // electron to react
    handleSuccessfulLogin: (callback) => ipcRenderer.on('handleSuccessfulLogin', callback),
    handleSuccessfulSpeckleUpload: (callback) => ipcRenderer.on('handleSuccessfulSpeckleUpload', callback),
    showLoadingPage: (callback) => ipcRenderer.on('showLoadingPage', callback),
    syncSessionData: (callback) => ipcRenderer.on('syncSessionData', callback),
    goHome: (callback) => ipcRenderer.on('goHome', callback),
    setUrls: (callback) => ipcRenderer.on('setUrls', callback),
    
    // electron to react remove listeners
    removeSuccessfulLoginHandler: () => ipcRenderer.removeAllListeners('handleSuccessfulLogin'),
    removeSuccessfulSpeckleUploadHandler: () => ipcRenderer.removeAllListeners('handleSuccessfulSpeckleUpload'),
    removeShowLoadingPageHandler: () => ipcRenderer.removeAllListeners('showLoadingPage'),
});