# How to

## Development

- Create a file named `.env` under `<root-directory>/electron-app` and copy the following:

```
PORT=3005
```

- Run `npm run start`. This will start the React server in dev mode
- Run `npm run ef-start` to launch the electron app in dev mode

## Deployment

- Run `npm run build`. This will build the optimized React package
- Update the version number
- Run `npm run ef-make` to build the exe. Will take a long time 😴

## Debugging

### Frontend

- To open debug console, focus on the electron app and type `open sesame` (no spaces).

### Backend

- Attach debugger to the process that spawns from running `npm run ef-start`.

- For VS Code users, add a file `.vscode/launch.json`. You can now directly run the electron application from VS Code instead of running the  `npm run ef-start` command:

```
// .vscode/launch.json

{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Main Process",
      "type": "node",
      "request": "launch",
      "cwd": "${workspaceFolder}",
      "runtimeExecutable": "${workspaceFolder}/node_modules/.bin/electron",
      "windows": {
        "runtimeExecutable": "${workspaceFolder}/node_modules/.bin/electron.cmd"
      },
      "args" : ["."],
      "outputCapture": "std"
    }
  ]
}
```
