# How to build

- Run `snaptrude-manager.iss` for fresh installers and `snaptrude-manager-update.iss` for update installers
- Update `installer version number` and the `dynamo scripts version` if necessary
- Set `WeWork` preprocessor directive to 1 or 0 depending on whom you're building the installer for
- In the update script, be sure to check which lines are uncommented
- Verify the installers being bundled
- Place the installers on the drive