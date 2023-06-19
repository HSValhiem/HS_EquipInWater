# Technical Writeup
## UseEquipmentInWater Performance Issue
- Reliance on stack trace: The Original UseEquipmentInWater code uses a StackTrace object to obtain method call information, specifically looking for method names containing "UpdateEquipment" or "EquipItem". Relying on stack trace for application logic is generally considered a poor practice. Stack traces are primarily intended for debugging purposes and should not be used for making critical decisions or controlling program flow. It introduces unnecessary complexity and can lead to unexpected behavior in different execution contexts or environments.

- Fragile code: The code checks for specific method names in the stack trace to determine if the code is being called from certain methods ("UpdateEquipment" or "EquipItem"). This approach is fragile because it tightly couples the code's behavior to the method names, making it highly susceptible to breaking if method names change or if the code is called from different methods in the future. It can easily result in bugs and maintenance issues.

- Lack of clarity and maintainability: The code's intention and purpose are not clear. It is not immediately apparent why the stack trace is being used and what the specific conditions signify. This lack of clarity can make the code difficult to understand, debug, and maintain for other developers working on the codebase.

- Performance impact: Obtaining a stack trace can have a performance impact, especially when performed frequently or in performance-critical sections of code. Constructing a stack trace involves collecting and processing information about the call stack, which can be computationally expensive. In this case, the stack trace is being created for every execution of the code block, potentially impacting overall performance.

- Overall, using stack traces for application logic, relying on specific method names, and introducing unnecessary complexity can lead to code that is difficult to understand, maintain, and debug. It is advisable to find alternative, more robust and maintainable approaches to achieve the desired behavior without relying on stack traces.

![image](https://github.com/HSValhiem/HS_EquipInWater/assets/18600015/6782f502-528b-4a88-aeba-387da4c4d5df)

## EquipInWater Performance
- Precision: Harmony transpilers allow you to directly target and modify specific methods or code blocks within the codebase. This provides a more precise and controlled approach to making changes, as you can specifically identify the exact locations where the modifications need to be applied.

- Compatibility: Harmony transpilers work by dynamically patching the code at runtime. This means that your modifications can adapt to changes in the original codebase, ensuring compatibility with updates or different versions of the software. In contrast, relying on stack trace can be fragile, as it tightly couples your modifications to specific method names that may change in the future.

- Maintainability: Harmony transpilers offer a modular and organized way to manage your code modifications. You can create separate transpiler patches for different modifications, making it easier to understand, maintain, and update your changes over time. On the other hand, using stack trace can lead to less maintainable code, as it may be harder to understand and debug for other developers.

- Performance: Harmony transpilers allow you to inject or change code directly at the IL (Intermediate Language) level. This can result in more efficient and optimized modifications compared to relying on stack trace, which involves additional computational overhead for obtaining and processing the stack trace information.

- Community Support: Harmony transpilers are widely used in modding communities and have a well-established ecosystem. Many resources, tutorials, and examples are available to help you leverage harmony transpilers effectively. Additionally, modding frameworks often provide support and compatibility for harmony patches, enabling better integration with other mods.

![image](https://github.com/HSValhiem/HS_EquipInWater/assets/18600015/69fa0588-9a3d-4cc4-9985-19e7a0728426)

# ThunderStore

# Equip in Water
It appears that EquipInWater is ready to take the spotlight among mods that enable equipment usage while swimming.
This mod is based on LVH-IT's "UseEquipmentInWater" mod, utilizing the same configuration schema for seamless migration.
Now, let's dive into the exciting details!
The significant change in EquipInWater is its enhanced performance compared to "UseEquipmentInWater." During performance benchmarking, it was discovered that "UseEquipmentInWater" consumed 5.9% of the main loop due to its non-production-ready methods.
In contrast, EquipInWater offers over 50 times better performance while also providing configuration options for water-usable items.

## Thanks!
Special thanks to LVH-IT for their exemplary mod, which served as a valuable reference and inspiration for this project. Their dedication in creating and maintaining such a long-standing mod is truly commendable.

## Beta Version Notice:

Please note that the current version of this mod, **EquipInWater**, is in beta stage. While we have thoroughly tested its functionality, there might still be some unforeseen issues or bugs.

We greatly appreciate your support and encourage you to report any issues you encounter during your gameplay. Your feedback is crucial in helping us improve the mod and provide a better experience for all users.

To report any issues or bugs, please visit our [GitHub Issues page](https://github.com/HSValhiem/HS_EquipInWater/issues). We kindly ask you to provide detailed information about the problem you encountered, including steps to reproduce it if possible. Our team will promptly review and address your reported issues.

Thank you for your understanding and cooperation in making EquipInWater the best it can be!

## Installation (Automatic)
1. Install the mod manager [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/).
2. Use the mod manager to install the mod by directly importing it.
3. Launch the game.

## Installation (Manual)
1. Install the [BepInExPack Valheim](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) mod.
2. Extract the mod's DLL file, `"HS_EquipInWater.dll"`, into the following folder: `<Steam Location>\steamapps\common\Valheim\BepInEx\plugins`
3. Launch the game.

## Configuration
The configuration file allows you to specify which items can be used in water and which ones cannot. You can access the configuration through the following methods:

### With r2modman Mod Manager:
1. Open the mod manager and navigate to the "Config Editor" menu.
2. Modify the configuration settings according to your preferences.

### With Manual Installation:
1. Locate the configuration file in this folder: `<Steam Location>\steamapps\common\Valheim\BepInEx\config`
2. Open the file named `"hs_equipinwater.cfg"` using a text editor.
3. Edit the configuration settings to your liking.

Make sure to save the changes to the configuration file before launching the game.
