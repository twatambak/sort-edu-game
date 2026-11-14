# Build Assistant

Open the tool from **Tools > Build Assistant**. On first open, it creates
`BuildAssistantSettings.asset` under `Assets/Editor/BuildAssistant/`.

The asset stores the product name, semantic version, build number, output path,
quick-build presets, scene selections, development-build preference and release
notes. Commit the asset to share the same build configuration with the team.

Each preset targets Windows 64-bit, Linux 64-bit, macOS, Android, iOS or WebGL.
Assign a Unity 6 Build Profile to activate its platform settings before a build.
If Unity must change platforms or reimport assets, the queued build resumes after
the editor completes that switch.
If the preset scene list is empty, the tool uses the active profile's scene
override when enabled, or the project's enabled Build Settings scenes.
Import Build Settings scenes into a preset to edit its scene list independently.

The output folder can be project-relative or absolute. Builds are placed under a
platform-specific subfolder. Android presets can generate APK or AAB files; iOS
presets export an Xcode project. The required Unity platform modules must be
installed, and iOS builds remain subject to the host platform and Unity
limitations. Android signing uses the signing configuration already set in
Player Settings; this tool does not store passwords or keystore credentials.

Only successful builds are added to the changelog and increment the configured
version/build number. Draft notes are registered against the version that was
built. The default version increment is patch. Mobile builds
initiated here use this asset's version and build number, even when the project
also has another mobile settings profile.
