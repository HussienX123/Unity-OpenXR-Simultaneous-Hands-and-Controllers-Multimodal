# Contributing

Contributions, bug reports, and device verification reports are welcome!

## Reporting Device Verification Results

Because this package is currently **Experimental, pending device verification**, test reports from both **Windows Editor (over Meta Quest Link)** and **Android builds on Meta Quest headsets** are especially helpful. When submitting a report:

1. Use the **Bug Report** issue template.
2. Include your exact headset model, Quest OS version, Unity version, and package version.
3. Include the exact `[MetaSimultaneousHandsAndControllersFeature]` and `[XRDiagnostics]` log lines from the Console or `adb logcat`.

## Pull Requests

1. Fork the repository and create a branch from `main`.
2. Keep changes focused and minimal.
3. Do not commit `Assets/Samples/`, `Library/`, build outputs, or signing credentials.
4. Ensure all runtime, editor, and sample scripts compile with zero errors and warnings on Unity `6000.0`+.
5. Open a Pull Request and complete the PR checklist.
