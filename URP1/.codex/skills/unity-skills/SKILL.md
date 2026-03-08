---
name: unity-skills
description: "Unity Editor automation via REST API. Control GameObjects, components, scenes, materials, prefabs, lights, and more with 100+ professional tools."
---

# Unity Editor Control Skill

You are an expert Unity developer. This skill enables you to directly control Unity Editor through a REST API.
Use the Python helper script in `scripts/unity_skills.py` to execute Unity operations.

## Prerequisites

1. Unity Editor must be running with the UnitySkills package installed
2. REST server must be started: **Window > UnitySkills > Start Server**
3. Server endpoint: `http://localhost:8090`

## Quick Start

```python
# Import the helper from the scripts/ directory
import sys
sys.path.insert(0, 'scripts')  # Adjust path to skill's scripts directory
from unity_skills import call_skill, is_unity_running, wait_for_unity

# Check if Unity is ready
if is_unity_running():
    # Create a cube
    call_skill('gameobject_create', name='MyCube', primitiveType='Cube', x=0, y=1, z=0)
    # Set its color to red
    call_skill('material_set_color', name='MyCube', r=1, g=0, b=0)
```

## ⚠️ Important: Script Creation & Domain Reload

When creating C# scripts with `script_create`, Unity recompiles all scripts (Domain Reload).
The server temporarily stops during compilation and auto-restarts.

```python
# After creating a script, wait for Unity to recompile
result = call_skill('script_create', name='MyScript', template='MonoBehaviour')
if result.get('success'):
    wait_for_unity(timeout=10)  # Wait for server to come back
```

## Workflow Examples

### Create a Game Scene
```python
# 1. Create ground
call_skill('gameobject_create', name='Ground', primitiveType='Plane', x=0, y=0, z=0)
call_skill('gameobject_set_transform', name='Ground', scaleX=5, scaleY=1, scaleZ=5)

# 2. Create player
call_skill('gameobject_create', name='Player', primitiveType='Capsule', x=0, y=1, z=0)
call_skill('component_add', name='Player', componentType='Rigidbody')

# 3. Add lighting
call_skill('light_create', name='Sun', lightType='Directional', intensity=1.5)

# 4. Save the scene
call_skill('scene_save', scenePath='Assets/Scenes/GameScene.unity')
```

### Create UI Menu
```python
call_skill('ui_create_canvas', name='MainMenu')
call_skill('ui_create_text', name='Title', parent='MainMenu', text='My Game', fontSize=48)
call_skill('ui_create_button', name='PlayBtn', parent='MainMenu', text='Play', width=200, height=50)
```

## Available Skills

### Animator
- `animator_create_controller(name, folder)` - Create a new Animator Controller
- `animator_add_parameter(controllerPath, paramName, paramType, defaultFloat, defaultInt, defaultBool)` - Add a parameter to an Animator Controller
- `animator_get_parameters(controllerPath)` - Get all parameters from an Animator Controller
- `animator_set_parameter(name, instanceId, path, paramName, paramType, floatValue, intValue, boolValue)` - Set a parameter value on a GameObject's Animator (supports name/instanceId/path)
- `animator_play(name, instanceId, path, stateName, layer, normalizedTime)` - Play an animation state on a GameObject (supports name/instanceId/path)
- `animator_get_info(name, instanceId, path)` - Get Animator component information (supports name/instanceId/path)
- `animator_assign_controller(name, instanceId, path, controllerPath)` - Assign an Animator Controller to a GameObject (supports name/instanceId/path)
- `animator_list_states(controllerPath, layer)` - List all states in an Animator Controller layer
- `animator_add_state(controllerPath, stateName, clipPath, layer)` - Add a state to an Animator Controller layer
- `animator_add_transition(controllerPath, fromState, toState, layer, hasExitTime, duration)` - Add a transition between two states in an Animator Controller

### Asset
- `asset_import(sourcePath, destinationPath)` - Import an asset from external path
- `asset_delete(assetPath)` - Delete an asset
- `asset_move(sourcePath, destinationPath)` - Move or rename an asset
- `asset_import_batch(items)` - Import multiple assets. items: JSON array of {sourcePath, destinationPath}
- `asset_delete_batch(items)` - Delete multiple assets. items: JSON array of {path}
- `asset_move_batch(items)` - Move multiple assets. items: JSON array of {sourcePath, destinationPath}
- `asset_duplicate(assetPath)` - Duplicate an asset
- `asset_find(searchFilter, limit)` - Find assets by name, type, or label
- `asset_create_folder(folderPath)` - Create a new folder in Assets
- `asset_refresh()` - Refresh the Asset Database
- `asset_get_info(assetPath)` - Get information about an asset

### AssetImport
- `asset_reimport(assetPath)` - Force reimport of an asset
- `asset_reimport_batch(searchFilter, folder, limit)` - Reimport multiple assets matching a pattern
- `texture_set_import_settings(assetPath, maxSize, compression, readable, generateMipMaps, textureType)` - Set texture import settings (maxSize, compression, readable)
- `model_set_import_settings(assetPath, globalScale, importMaterials, importAnimation, generateColliders, readable, meshCompression)` - Set model (FBX) import settings
- `audio_set_import_settings(assetPath, forceToMono, loadInBackground, loadType, compressionFormat, quality)` - Set audio clip import settings
- `sprite_set_import_settings(assetPath, spriteMode, pixelsPerUnit, packingTag, pivotX, pivotY)` - Set sprite import settings (mode, pivot, packingTag, pixelsPerUnit)
- `texture_get_import_settings(assetPath)` - Get current texture import settings
- `model_get_import_settings(assetPath)` - Get current model import settings
- `audio_get_import_settings(assetPath)` - Get current audio import settings
- `asset_set_labels(assetPath, labels)` - Set labels on an asset
- `asset_get_labels(assetPath)` - Get labels of an asset

### Audio
- `audio_get_settings(assetPath)` - Get audio import settings for an audio asset
- `audio_set_settings(assetPath, forceToMono, loadInBackground, ambisonic, loadType, compressionFormat, quality, sampleRateSetting)` - Set audio import settings. loadType: DecompressOnLoad/CompressedInMemory/Streaming. compressionFormat: PCM/Vorbis/ADPCM. quality: 0.0-1.0
- `audio_set_settings_batch(items)` - Set audio import settings for multiple audio files. items: JSON array of {assetPath, forceToMono, loadType, compressionFormat, quality, ...}
- `audio_find_clips(filter, limit)` - Search for AudioClip assets in the project
- `audio_get_clip_info(assetPath)` - Get detailed information about an AudioClip asset
- `audio_add_source(name, instanceId, path, clipPath, playOnAwake, loop, volume)` - Add an AudioSource component to a GameObject
- `audio_get_source_info(name, instanceId, path)` - Get AudioSource configuration
- `audio_set_source_properties(name, instanceId, path, clipPath, volume, pitch, loop, playOnAwake, mute, spatialBlend, priority)` - Set AudioSource properties
- `audio_find_sources_in_scene(limit)` - Find all AudioSource components in the current scene
- `audio_create_mixer(mixerName, folder)` - Create a new AudioMixer asset

### Camera
- `camera_align_view_to_object(name, instanceId, path)` - Align Scene View camera to look at an object.
- `camera_get_info()` - Get Scene View camera position and rotation.
- `camera_set_transform(posX, posY, posZ, rotX, rotY, rotZ, size, instant)` - Set Scene View camera position/rotation manually.
- `camera_look_at(x, y, z)` - Focus Scene View camera on a point.
- `camera_create(name, x, y, z)` - Create a new Game Camera
- `camera_get_properties(name, instanceId, path)` - Get Game Camera properties (supports name/instanceId/path)
- `camera_set_properties(name, instanceId, path, fieldOfView, nearClipPlane, farClipPlane, depth, clearFlags, bgR, bgG, bgB)` - Set Game Camera properties (FOV, clip planes, clear flags, background color, depth)
- `camera_set_culling_mask(layerNames, name, instanceId, path)` - Set Game Camera culling mask by layer names (comma-separated)
- `camera_screenshot(savePath, width, height, name, instanceId, path)` - Capture a screenshot from a Game Camera to file
- `camera_set_orthographic(orthographic, orthographicSize, name, instanceId, path)` - Switch Game Camera between orthographic and perspective mode
- `camera_list()` - List all cameras in the scene

### Cinemachine
- `cinemachine_create_vcam(name, folder)` - Create a new Virtual Camera
- `cinemachine_inspect_vcam(vcamName, instanceId, path)` - Deeply inspect a VCam, returning fields and tooltips.
- `cinemachine_set_vcam_property(vcamName, instanceId, path, componentType, propertyName, value)` - Set any property on VCam or its pipeline components.
- `cinemachine_set_targets(vcamName, instanceId, path, followName, lookAtName)` - Set Follow and LookAt targets.
- `cinemachine_add_component(vcamName, instanceId, path, componentType)` - Add a Cinemachine component (e.g., OrbitalFollow).
- `cinemachine_set_lens(vcamName, instanceId, path, fov, nearClip, farClip, orthoSize, mode)` - Quickly configure Lens settings (FOV, Near, Far, OrthoSize).
- `cinemachine_list_components()` - List all available Cinemachine component names.
- `cinemachine_set_component(vcamName, instanceId, path, stage, componentType)` - Switch VCam pipeline component (Body/Aim/Noise). CM3 only.
- `cinemachine_impulse_generate(sourceParams)` - Trigger an Impulse. Params: {velocity: {x,y,z}} or empty.
- `cinemachine_get_brain_info()` - Get info about the Active Camera and Blend.
- `cinemachine_set_active(vcamName, instanceId, path)` - Force activation of a VCam (SOLO) by setting highest priority.
- `cinemachine_set_noise(vcamName, instanceId, path, amplitudeGain, frequencyGain)` - Configure Noise settings (Basic Multi Channel Perlin).
- `cinemachine_create_target_group(name)` - Create a CinemachineTargetGroup. Returns name.
- `cinemachine_target_group_add_member(groupName, groupInstanceId, groupPath, targetName, targetInstanceId, targetPath, weight, radius)` - Add/Update member in TargetGroup. Inputs: groupName, targetName, weight, radius.
- `cinemachine_target_group_remove_member(groupName, groupInstanceId, groupPath, targetName, targetInstanceId, targetPath)` - Remove member from TargetGroup. Inputs: groupName, targetName.
- `cinemachine_set_spline(vcamName, vcamInstanceId, vcamPath, splineName, splineInstanceId, splinePath)` - Set Spline for VCam Body. CM3 + Splines only. Inputs: vcamName, splineName.
- `cinemachine_add_extension(vcamName, instanceId, path, extensionName)` - Add a CinemachineExtension. Inputs: vcamName, extensionName (e.g. CinemachineStoryboard).
- `cinemachine_remove_extension(vcamName, instanceId, path, extensionName)` - Remove a CinemachineExtension. Inputs: vcamName, extensionName.
- `cinemachine_create_mixing_camera(name)` - Create a Cinemachine Mixing Camera.
- `cinemachine_mixing_camera_set_weight(mixerName, mixerInstanceId, mixerPath, childName, childInstanceId, childPath, weight)` - Set weight of a child camera in a Mixing Camera. Inputs: mixerName, childName, weight.
- `cinemachine_create_clear_shot(name)` - Create a Cinemachine Clear Shot Camera.
- `cinemachine_create_state_driven_camera(name, targetAnimatorName)` - Create a Cinemachine State Driven Camera. Optional: targetAnimatorName.
- `cinemachine_state_driven_camera_add_instruction(cameraName, cameraInstanceId, cameraPath, stateName, childCameraName, childInstanceId, childPath, minDuration, activateAfter)` - Add instruction to State Driven Camera. Inputs: cameraName, stateName, childCameraName, minDuration, activateAfter.

### Cleaner
- `cleaner_find_unused_assets(assetType, searchPath, limit)` - Find potentially unused assets of a specific type
- `cleaner_find_duplicates(assetType, searchPath, limit)` - Find duplicate files by content hash
- `cleaner_find_missing_references(includeInactive)` - Find components with missing script or asset references
- `cleaner_delete_assets(paths, confirmToken)` - Delete specified assets. Step 1: Call without confirmToken to preview. Step 2: Call with confirmToken to execute.
- `cleaner_get_asset_usage(assetPath, limit)` - Find what objects reference a specific asset
- `cleaner_find_empty_folders(searchPath)` - Find empty folders in the project
- `cleaner_find_large_assets(searchPath, limit, minSizeBytes)` - Find largest assets by file size
- `cleaner_delete_empty_folders(searchPath)` - Delete all empty folders
- `cleaner_fix_missing_scripts(includeInactive)` - Remove missing script components from GameObjects
- `cleaner_get_dependency_tree(assetPath, recursive)` - Get dependency tree for an asset

### Component
- `component_add(name, instanceId, path, componentType)` - Add a component to a GameObject (supports name/instanceId/path). Works with Cinemachine, TextMeshPro, etc.
- `component_add_batch(items)` - Add components to multiple GameObjects. items: JSON array of {name, componentType, path}
- `component_remove(name, instanceId, path, componentType, componentIndex)` - Remove a component from a GameObject (supports name/instanceId/path)
- `component_remove_batch(items)` - Remove components from multiple GameObjects. items: JSON array of {name, componentType, path}
- `component_list(name, instanceId, path, includeProperties)` - List all components on a GameObject with detailed info (supports name/instanceId/path)
- `component_set_property(name, instanceId, path, componentType, propertyName, value, referencePath, referenceName)` - Set a property/field on a component. Supports Vector2/3/4, Color, references by name/path
- `component_set_property_batch(items)` - Set properties on multiple components (Efficient). items: JSON array of {name, componentType, propertyName, value, referencePath, referenceName}
- `component_get_properties(name, instanceId, path, componentType, includePrivate)` - Get all properties of a component (supports name/instanceId/path)
- `component_copy(sourceName, sourceInstanceId, sourcePath, targetName, targetInstanceId, targetPath, componentType)` - Copy a component from one GameObject to another
- `component_set_enabled(name, instanceId, path, componentType, enabled)` - Enable or disable a component (Behaviour, Renderer, Collider, etc.)

### Console
- `console_start_capture()` - Start capturing console logs
- `console_stop_capture()` - Stop capturing console logs
- `console_get_logs(type, filter, limit)` - Get Unity Console logs. Reads existing console history directly (no setup needed). Use type=All/Error/Warning/Log to filter. When console_start_capture is active, returns captured logs with timestamps instead.
- `console_clear()` - Clear the Unity console
- `console_log(message, type)` - Write a message to the console
- `console_set_pause_on_error(enabled)` - Enable or disable Error Pause in Play mode
- `console_export(savePath)` - Export console logs to a file. Uses captured buffer when console_start_capture is active; otherwise reads directly from Unity Console history (no setup needed).
- `console_get_stats()` - Get log statistics (count by type). Uses captured buffer when console_start_capture is active; otherwise reads directly from Unity Console history.
- `console_set_collapse(enabled)` - Set console log collapse mode
- `console_set_clear_on_play(enabled)` - Set clear on play mode

### Debug
- `debug_get_errors(limit)` - Get only errors and exceptions from Unity Console logs. Reads existing console history directly (no setup needed). For all log types use console_get_logs.
- `debug_get_logs(type, filter, limit)` - Get console logs filtered by type (Error/Warning/Log) and content. Reads existing console history directly (no setup needed). Prefer console_get_logs for all-type queries with timestamp support.
- `debug_check_compilation()` - Check if Unity is currently compiling scripts.
- `debug_force_recompile()` - Force script recompilation.
- `debug_get_system_info()` - Get Editor and System capabilities.
- `debug_get_stack_trace(entryIndex)` - Get stack trace for a log entry by index
- `debug_get_assembly_info()` - Get project assembly information
- `debug_get_defines()` - Get scripting define symbols for current platform
- `debug_set_defines(defines)` - Set scripting define symbols for current platform
- `debug_get_memory_info()` - Get memory usage information

### Editor
- `editor_play()` - Enter play mode. Warning: any unsaved scene changes made during Play mode will be lost when exiting.
- `editor_stop()` - Exit play mode. Warning: any scene changes made during Play mode will be lost.
- `editor_pause()` - Pause/unpause play mode
- `editor_select(name, instanceId, path)` - Select a GameObject
- `editor_get_selection()` - Get currently selected objects
- `editor_undo()` - Undo the last action (single step). For multiple undo steps use history_undo(steps=N). For workflow-level undo use workflow_undo_task.
- `editor_redo()` - Redo the last undone action (single step). For multiple redo steps use history_redo(steps=N).
- `editor_get_state()` - Get current editor state
- `editor_execute_menu(menuPath)` - Execute a Unity menu item
- `editor_get_tags()` - Get all available tags
- `editor_get_layers()` - Get all available layers
- `editor_get_context(includeComponents, includeChildren)` - Get full editor context - selected GameObjects, selected assets, active scene, focused window. Use this to get current selection without searching.

### Event
- `event_get_listeners(name, instanceId, path, componentName, eventName)` - Get persistent listeners of a UnityEvent
- `event_add_listener(name, instanceId, path, componentName, eventName, targetObjectName, targetComponentName, methodName, mode, argType, floatArg, intArg, stringArg, boolArg)` - Add a persistent listener to a UnityEvent (Editor time). Supported args: void, int, float, string, bool, Object.
- `event_remove_listener(name, instanceId, path, componentName, eventName, index)` - Remove a persistent listener by index
- `event_invoke(name, instanceId, path, componentName, eventName)` - Invoke a UnityEvent explicitly (Runtime only)
- `event_clear_listeners(name, instanceId, path, componentName, eventName)` - Remove all persistent listeners from a UnityEvent
- `event_set_listener_state(name, instanceId, path, componentName, eventName, index, state)` - Set a listener's call state (Off, RuntimeOnly, EditorAndRuntime)
- `event_list_events(name, instanceId, path, componentName)` - List all UnityEvent fields on a component
- `event_add_listener_batch(name, instanceId, path, componentName, eventName, items)` - Add multiple listeners at once. items: JSON array of {targetObjectName, targetComponentName, methodName}
- `event_copy_listeners(sourceObject, sourceComponent, sourceEvent, targetObject, targetComponent, targetEvent)` - Copy listeners from one event to another
- `event_get_listener_count(name, instanceId, path, componentName, eventName)` - Get the number of persistent listeners on a UnityEvent

### GameObject
- `gameobject_create_batch(items)` - Create multiple GameObjects in one call (Efficient). items: JSON array of {name, primitiveType, x, y, z}
- `gameobject_create(name, primitiveType, x, y, z)` - Create a new GameObject. primitiveType: Cube, Sphere, Capsule, Cylinder, Plane, Quad, or Empty/null for empty object
- `gameobject_rename(name, instanceId, path, newName)` - Rename a GameObject (supports name/instanceId/path). Returns: {success, oldName, newName, instanceId}
- `gameobject_rename_batch(items)` - Rename multiple GameObjects in one call (Efficient). items: JSON array of {name, instanceId, path, newName}. Returns array with oldName, newName for each.
- `gameobject_delete(name, instanceId, path)` - Delete a GameObject (supports name/instanceId/path)
- `gameobject_delete_batch(items)` - Delete multiple GameObjects. items: JSON array of strings (names) or objects {name, instanceId, path}
- `gameobject_find(name, useRegex, tag, layer, component, limit)` - Find GameObjects by name/regex, tag, layer, or component
- `gameobject_set_transform(name, instanceId, path, posX, posY, posZ, rotX, rotY, rotZ, scaleX, scaleY, scaleZ, localPosX, localPosY, localPosZ, anchoredPosX, anchoredPosY, anchorMinX, anchorMinY, anchorMaxX, anchorMaxY, pivotX, pivotY, sizeDeltaX, sizeDeltaY, width, height)` - Set transform properties. For UI/RectTransform: use anchorX/Y, pivotX/Y, sizeDeltaX/Y. For 3D: use posX/Y/Z, rotX/Y/Z, scaleX/Y/Z
- `gameobject_set_transform_batch(items)` - Set transform properties for multiple objects (Efficient). items: JSON array of objects with optional fields (name, posX, rotX, scaleX, etc.)
- `gameobject_duplicate(name, instanceId, path)` - Duplicate a GameObject (supports name/instanceId/path). Returns: originalName, copyName, copyInstanceId, copyPath
- `gameobject_duplicate_batch(items)` - Duplicate multiple GameObjects in one call (Efficient). items: JSON array of {name, instanceId, path}. Returns array with originalName, copyName, copyInstanceId for each.
- `gameobject_set_parent(childName, childInstanceId, childPath, parentName, parentInstanceId, parentPath)` - Set the parent of a GameObject (supports name/instanceId/path)
- `gameobject_get_info(name, instanceId, path)` - Get detailed info about a GameObject (supports name/instanceId/path)
- `gameobject_set_active(name, instanceId, path, active)` - Enable or disable a GameObject (supports name/instanceId/path)
- `gameobject_set_active_batch(items)` - Enable or disable multiple GameObjects. items: JSON array of {name, active}
- `gameobject_set_layer_batch(items)` - Set layer for multiple GameObjects. items: JSON array of {name, layer, recursive}
- `gameobject_set_tag_batch(items)` - Set tag for multiple GameObjects. items: JSON array of {name, tag}
- `gameobject_set_parent_batch(items)` - Set parent for multiple GameObjects. items: JSON array of {childName, parentName, ...}

### Light
- `light_create(name, lightType, x, y, z, r, g, b, intensity, range, spotAngle, shadows)` - Create a new light (Directional, Point, Spot, Area)
- `light_set_properties(name, instanceId, path, r, g, b, intensity, range, spotAngle, shadows)` - Set light properties (supports name/instanceId/path)
- `light_get_info(name, instanceId, path)` - Get information about a light (supports name/instanceId/path)
- `light_find_all(lightType, limit)` - Find all lights in the scene
- `light_set_enabled(name, instanceId, path, enabled)` - Enable or disable a light (supports name/instanceId/path). Returns: {success, name, enabled}
- `light_set_enabled_batch(items)` - Enable/disable multiple lights in one call (Efficient). items: JSON array of {name, instanceId, path, enabled}
- `light_set_properties_batch(items)` - Set properties for multiple lights in one call (Efficient). items: JSON array of {name, instanceId, r, g, b, intensity, range, shadows}
- `light_add_probe_group(name, instanceId, path, gridX, gridY, gridZ, spacingX, spacingY, spacingZ)` - Add a Light Probe Group to a GameObject. Optional grid layout: gridX/gridY/gridZ (count per axis), spacingX/spacingY/spacingZ (meters between probes)
- `light_add_reflection_probe(probeName, x, y, z, sizeX, sizeY, sizeZ, resolution)` - Create a Reflection Probe at a position
- `light_get_lightmap_settings()` - Get Lightmap baking settings

### Material
- `material_create(name, shaderName, savePath)` - Create a new material (auto-detects render pipeline if shader not specified). savePath can be a folder or full path.
- `material_assign(name, instanceId, path, materialPath)` - Assign a material asset to a renderer (supports name/instanceId/path)
- `material_create_batch(items)` - Create multiple materials (Efficient). items: JSON array of {name, shaderName?, savePath?}
- `material_assign_batch(items)` - Assign materials to multiple objects (Efficient). items: JSON array of {name, materialPath}
- `material_duplicate(sourcePath, newName, savePath)` - Duplicate an existing material
- `material_set_color(name, instanceId, path, r, g, b, a, propertyName, intensity)` - Set a color property on a material with optional HDR intensity for emission
- `material_set_colors_batch(items, propertyName)` - Set colors on multiple GameObjects in a single call. items is a JSON array like [{name:'Obj1',r:1,g:0,b:0},{name:'Obj2',r:0,g:1,b:0}]. Much more efficient than calling material_set_color multiple times.
- `material_set_emission(name, instanceId, path, r, g, b, intensity, enableEmission)` - Set emission color with HDR intensity and auto-enable emission
- `material_set_emission_batch(items)` - Set emission on multiple objects (Efficient). items: JSON array of {name, r, g, b, intensity?, enableEmission?}
- `material_set_texture(name, instanceId, path, texturePath, propertyName)` - Set a texture on a material (auto-detects property name for render pipeline)
- `material_set_float(name, instanceId, path, propertyName, value)` - Set a float property on a material
- `material_set_int(name, instanceId, path, propertyName, value)` - Set an integer property on a material
- `material_set_vector(name, instanceId, path, propertyName, x, y, z, w)` - Set a vector4 property on a material
- `material_set_texture_offset(name, instanceId, path, propertyName, x, y)` - Set texture offset (tiling position)
- `material_set_texture_scale(name, instanceId, path, propertyName, x, y)` - Set texture scale (tiling)
- `material_set_keyword(name, instanceId, path, keyword, enable)` - Enable or disable a shader keyword (e.g., _EMISSION, _NORMALMAP, _METALLICGLOSSMAP)
- `material_set_render_queue(name, instanceId, path, renderQueue)` - Set material render queue (-1 for shader default, 2000=Geometry, 2450=AlphaTest, 3000=Transparent)
- `material_set_shader(name, instanceId, path, shaderName)` - Change the shader of a material
- `material_set_gi_flags(name, instanceId, path, flags)` - Set global illumination flags (None, RealtimeEmissive, BakedEmissive, EmissiveIsBlack)
- `material_get_properties(name, instanceId, path)` - Get all properties of a material (colors, floats, textures, keywords)
- `material_get_keywords(name, instanceId, path)` - Get all enabled shader keywords on a material

### Model
- `model_get_settings(assetPath)` - Get model import settings for a 3D model asset (FBX, OBJ, etc)
- `model_set_settings(assetPath, globalScale, useFileScale, importBlendShapes, importVisibility, importCameras, importLights, meshCompression, isReadable, optimizeMeshPolygons, optimizeMeshVertices, generateSecondaryUV, keepQuads, weldVertices, importNormals, importTangents, animationType, importAnimation, materialImportMode)` - Set model import settings. meshCompression: Off/Low/Medium/High. animationType: None/Legacy/Generic/Humanoid. materialImportMode: None/ImportViaMaterialDescription/ImportStandard
- `model_set_settings_batch(items)` - Set model import settings for multiple 3D models. items: JSON array of {assetPath, meshCompression, animationType, ...}
- `model_find_assets(filter, limit)` - Search for model assets in the project
- `model_get_mesh_info(name, instanceId, path, assetPath)` - Get detailed Mesh information (vertices, triangles, submeshes)
- `model_get_materials_info(assetPath)` - Get material mapping for a model asset
- `model_get_animations_info(assetPath)` - Get animation clip information from a model asset
- `model_set_animation_clips(assetPath, clips)` - Configure animation clip splitting. clips: JSON array of {name, firstFrame, lastFrame, loop}
- `model_get_rig_info(assetPath)` - Get rig/skeleton binding information
- `model_set_rig(assetPath, animationType, avatarSetup)` - Set rig/skeleton binding type. animationType: None/Legacy/Generic/Humanoid

### NavMesh
- `navmesh_bake()` - Bake the NavMesh (Synchronous). Warning: Can be slow.
- `navmesh_clear()` - Clear the NavMesh data
- `navmesh_calculate_path(startX, startY, startZ, endX, endY, endZ, areaMask)` - Calculate a path between two points. Returns: {status, distance, cornerCount, corners}
- `navmesh_add_agent(name, instanceId, path)` - Add NavMeshAgent component to an object
- `navmesh_set_agent(name, instanceId, path, speed, acceleration, angularSpeed, radius, height, stoppingDistance)` - Set NavMeshAgent properties (speed, acceleration, radius, height, stoppingDistance)
- `navmesh_add_obstacle(name, instanceId, path, carve)` - Add NavMeshObstacle component to an object
- `navmesh_set_obstacle(name, instanceId, path, shape, sizeX, sizeY, sizeZ, carving)` - Set NavMeshObstacle properties (shape, size, carving)
- `navmesh_sample_position(x, y, z, maxDistance)` - Find nearest point on NavMesh
- `navmesh_set_area_cost(areaIndex, cost)` - Set area traversal cost
- `navmesh_get_settings()` - Get NavMesh build settings

### Optimization
- `optimize_textures(maxTextureSize, enableCrunch, compressionQuality, filter)` - Optimize texture settings (maxSize, compression). Returns list of modified textures.
- `optimize_mesh_compression(compressionLevel, filter)` - Set mesh compression for 3D models
- `optimize_analyze_scene(polyThreshold, materialThreshold)` - Analyze scene for performance bottlenecks (high-poly meshes, excessive materials)
- `optimize_find_large_assets(thresholdKB, assetType, limit)` - Find assets exceeding a size threshold (in KB)
- `optimize_set_static_flags(name, instanceId, path, flags, includeChildren)` - Set static flags on GameObjects. flags: Everything/Nothing/BatchingStatic/OccludeeStatic/OccluderStatic/NavigationStatic/ReflectionProbeStatic
- `optimize_get_static_flags(name, instanceId, path)` - Get static flags of a GameObject
- `optimize_audio_compression(compressionFormat, loadType, quality, filter)` - Batch set audio compression. compressionFormat: PCM/Vorbis/ADPCM. loadType: DecompressOnLoad/CompressedInMemory/Streaming
- `optimize_find_duplicate_materials(limit)` - Find materials with identical shader and properties
- `optimize_analyze_overdraw(limit)` - Analyze transparent objects that may cause overdraw
- `optimize_set_lod_group(name, instanceId, path, lodDistances)` - Add or configure LOD Group. lodDistances: comma-separated screen-relative heights (e.g. '0.6,0.3,0.1')

### Package
- `package_list()` - List all installed packages
- `package_check(packageId)` - Check if a package is installed. Returns version if installed.
- `package_install(packageId, version)` - Install a package. version is optional.
- `package_remove(packageId)` - Remove an installed package.
- `package_refresh()` - Refresh the installed package list cache.
- `package_install_cinemachine(version)` - Install Cinemachine. version: 2 or 3 (default 3). CM3 auto-installs Splines dependency.
- `package_install_splines()` - Install Unity Splines package. Auto-detects correct version for Unity 6 vs Unity 2022.
- `package_get_cinemachine_status()` - Get Cinemachine installation status.
- `package_search(query)` - Search for packages in the Unity Registry
- `package_get_dependencies(packageId)` - Get dependency list for an installed package
- `package_get_versions(packageId)` - Get all available versions for a package

### Perception
- `scene_summarize(includeComponentStats, topComponentsLimit)` - Get a structured summary of the current scene (object counts, component stats, hierarchy depth)
- `hierarchy_describe(maxDepth, includeInactive, maxItemsPerLevel)` - Get a text tree of the scene hierarchy (like 'tree' command). Returns human-readable text. For JSON structure use scene_get_hierarchy.
- `script_analyze(scriptName, includePrivate)` - Analyze a script's public API (MonoBehaviour, ScriptableObject, or plain class)
- `scene_spatial_query(x, y, z, radius, nearObject, componentFilter, maxResults)` - Find objects within a radius of a point, or near another object
- `scene_materials(includeProperties)` - Get an overview of all materials and shaders used in the current scene
- `scene_context(maxDepth, maxObjects, rootPath, includeValues, includeReferences)` - Generate a comprehensive scene snapshot for AI coding assistance (hierarchy, components, script fields, references, UI layout). Best for initial context gathering before editing code or complex scene work.
- `scene_export_report(savePath, maxDepth, maxObjects)` - Export complete scene structure and script dependency report as markdown file. Use when user asks to: export scene report, generate scene document, save scene overview, create scene context file
- `scene_dependency_analyze(targetPath, savePath)` - Analyze object dependency graph and impact of changes. Use ONLY when user explicitly asks about: dependency analysis, impact analysis, what depends on, what references, safe to delete/disable/remove, refactoring impact, reference check
- `scene_tag_layer_stats()` - Get Tag/Layer usage stats and find potential issues (untagged objects, unused layers)
- `scene_performance_hints()` - Diagnose scene performance issues with prioritized actionable suggestions

### Physics
- `physics_raycast(originX, originY, originZ, dirX, dirY, dirZ, maxDistance, layerMask)` - Cast a ray and get hit info. Returns: {hit, collider, point, normal, distance}
- `physics_check_overlap(x, y, z, radius, layerMask)` - Check for colliders in a sphere. Returns list of hit colliders.
- `physics_get_gravity()` - Get global gravity setting
- `physics_set_gravity(x, y, z)` - Set global gravity setting
- `physics_raycast_all(originX, originY, originZ, dirX, dirY, dirZ, maxDistance, layerMask)` - Cast a ray and return ALL hits (penetrating)
- `physics_spherecast(originX, originY, originZ, dirX, dirY, dirZ, radius, maxDistance, layerMask)` - Cast a sphere along a direction and get hit info
- `physics_boxcast(originX, originY, originZ, dirX, dirY, dirZ, halfExtentX, halfExtentY, halfExtentZ, maxDistance, layerMask)` - Cast a box along a direction and get hit info
- `physics_overlap_box(x, y, z, halfExtentX, halfExtentY, halfExtentZ, layerMask)` - Check for colliders overlapping a box volume
- `physics_create_material(name, savePath, dynamicFriction, staticFriction, bounciness)` - Create a PhysicMaterial asset
- `physics_set_material(materialPath, name, instanceId, path)` - Set PhysicMaterial on a collider (supports name/instanceId/path)
- `physics_get_layer_collision(layer1, layer2)` - Get whether two layers collide
- `physics_set_layer_collision(layer1, layer2, enableCollision)` - Set whether two layers collide

### Prefab
- `prefab_create(name, instanceId, path, savePath)` - Create a prefab from a GameObject
- `prefab_instantiate(prefabPath, x, y, z, name)` - Instantiate a prefab in the scene
- `prefab_instantiate_batch(items)` - Instantiate multiple prefabs (Efficient). items: JSON array of {prefabPath, x, y, z, name, rotX, rotY, rotZ, scaleX, scaleY, scaleZ}
- `prefab_apply(name, instanceId, path)` - Apply all overrides from prefab instance to the source prefab asset. Equivalent to prefab_apply_overrides.
- `prefab_unpack(name, instanceId, path, completely)` - Unpack a prefab instance. completely=false: unpack outermost root only; completely=true: fully unpack all nested prefabs.
- `prefab_get_overrides(name, instanceId)` - Get list of property overrides on a prefab instance
- `prefab_revert_overrides(name, instanceId)` - Revert all overrides on a prefab instance back to prefab values
- `prefab_apply_overrides(name, instanceId)` - Apply all overrides from instance to source prefab asset. Equivalent to prefab_apply.
- `prefab_create_variant(sourcePrefabPath, variantPath)` - Create a prefab variant from an existing prefab
- `prefab_find_instances(prefabPath, limit)` - Find all instances of a prefab in the current scene

### Profiler
- `profiler_get_stats()` - Get performance statistics (FPS, Memory, Batches)
- `profiler_get_memory()` - Get memory usage overview (total allocated, reserved, mono heap)
- `profiler_get_runtime_memory(limit)` - Get top N objects by runtime memory usage in the scene
- `profiler_get_texture_memory(limit)` - Get memory usage of all loaded textures
- `profiler_get_mesh_memory(limit)` - Get memory usage of all loaded meshes
- `profiler_get_material_memory(limit)` - Get memory usage of all loaded materials
- `profiler_get_audio_memory(limit)` - Get memory usage of all loaded AudioClips
- `profiler_get_object_count(topN)` - Count all loaded objects grouped by type
- `profiler_get_rendering_stats()` - Get rendering statistics (batches, triangles, vertices, etc.)
- `profiler_get_asset_bundle_stats()` - Get information about all loaded AssetBundles

### Project
- `project_get_info()` - Get project information including render pipeline, Unity version, and settings
- `project_get_render_pipeline()` - Get current render pipeline type and recommended shaders
- `project_list_shaders(filter, limit)` - List all available shaders in the project
- `project_get_quality_settings()` - Get current quality settings
- `project_get_build_settings()` - Get build settings (platform, scenes)
- `project_get_packages()` - List installed UPM packages
- `project_get_layers()` - Get all Layer definitions
- `project_get_tags()` - Get all Tag definitions
- `project_add_tag(tagName)` - Add a custom Tag
- `project_get_player_settings()` - Get Player Settings
- `project_set_quality_level(level, levelName)` - Switch quality level by index or name

### Sample
- `create_cube(x, y, z, name)` - Create a cube at the specified position
- `create_sphere(x, y, z, name)` - Create a sphere at the specified position
- `delete_object(objectName)` - Delete a GameObject by name
- `get_scene_info()` - Get current scene information
- `set_object_position(objectName, x, y, z)` - Set position of a GameObject
- `set_object_rotation(objectName, x, y, z)` - Set rotation of a GameObject (Euler angles)
- `set_object_scale(objectName, x, y, z)` - Set scale of a GameObject
- `find_objects_by_name(nameContains)` - Find all GameObjects containing a name (param: nameContains)

### Scene
- `scene_create(scenePath)` - Create a new empty scene
- `scene_load(scenePath, additive)` - Load an existing scene
- `scene_save(scenePath)` - Save the current scene
- `scene_get_info()` - Get current scene information
- `scene_get_hierarchy(maxDepth)` - Get scene hierarchy tree
- `scene_screenshot(filename, width, height)` - Capture a screenshot of the game view. filename is a bare filename only (no path separators); saved under Assets/Screenshots/.
- `scene_get_loaded()` - Get list of all currently loaded scenes
- `scene_unload(sceneName)` - Unload a loaded scene (additive)
- `scene_set_active(sceneName)` - Set the active scene (for multi-scene editing)
- `scene_find_objects(namePattern, tag, componentType, limit)` - Search GameObjects by name pattern, tag, or component type. For advanced search (regex, layer, path) use gameobject_find.

### Script
- `script_create(scriptName, name, folder, template, namespaceName)` - Create a new C# script. Optional: namespace
- `script_create_batch(items)` - Create multiple scripts (Efficient). items: JSON array of {scriptName, folder, template, namespace}
- `script_read(scriptPath)` - Read the contents of a script
- `script_delete(scriptPath)` - Delete a script file
- `script_find_in_file(pattern, folder, isRegex, limit)` - Search for pattern in scripts
- `script_append(scriptPath, content, atLine)` - Append content to a script
- `script_replace(scriptPath, find, replace, isRegex)` - Find and replace content in a script file
- `script_list(folder, filter, limit)` - List C# script files in the project
- `script_get_info(scriptPath)` - Get script info (class name, base class, methods)
- `script_rename(scriptPath, newName)` - Rename a script file
- `script_move(scriptPath, newFolder)` - Move a script to a new folder

### ScriptableObject
- `scriptableobject_create(typeName, savePath)` - Create a new ScriptableObject asset
- `scriptableobject_get(assetPath)` - Get properties of a ScriptableObject
- `scriptableobject_set(assetPath, fieldName, value)` - Set a field/property on a ScriptableObject
- `scriptableobject_list_types(filter, limit)` - List available ScriptableObject types
- `scriptableobject_duplicate(assetPath)` - Duplicate a ScriptableObject asset
- `scriptableobject_set_batch(assetPath, fields)` - Set multiple fields on a ScriptableObject at once. fields: JSON object {fieldName: value, ...}
- `scriptableobject_delete(assetPath)` - Delete a ScriptableObject asset
- `scriptableobject_find(typeName, searchPath, limit)` - Find ScriptableObject assets by type name
- `scriptableobject_export_json(assetPath, savePath)` - Export a ScriptableObject to JSON
- `scriptableobject_import_json(assetPath, json, jsonFilePath)` - Import JSON data into a ScriptableObject

### Shader
- `shader_create(shaderName, savePath, template)` - Create a new shader file
- `shader_read(shaderPath)` - Read shader source code
- `shader_list(filter, limit)` - List all shaders in project
- `shader_get_properties(shaderNameOrPath)` - Get properties of a shader
- `shader_find(searchName)` - Find shaders by name
- `shader_delete(shaderPath)` - Delete a shader file
- `shader_check_errors(shaderNameOrPath)` - Check shader for compilation errors
- `shader_get_keywords(shaderNameOrPath)` - Get shader keyword list
- `shader_get_variant_count(shaderNameOrPath)` - Get shader variant count for performance analysis
- `shader_create_urp(shaderName, savePath, type)` - Create a URP shader from template (type: Unlit or Lit)
- `shader_set_global_keyword(keyword, enabled)` - Enable or disable a global shader keyword

### Smart
- `smart_scene_query(componentName, propertyName, op, value, limit)` - Query objects by component property (params: componentName, propertyName, op, value). e.g. componentName='Light', propertyName='intensity', op='>', value='10'
- `smart_scene_layout(layoutType, axis, spacing, columns, arcAngle, lookAtCenter)` - Organize selected objects into a layout (Linear, Grid, Circle, Arc). Requires objects selected in Hierarchy first.
- `smart_reference_bind(targetName, componentName, fieldName, sourceTag, sourceName, appendMode)` - Auto-fill a List/Array field with objects matching tag or name pattern
- `smart_scene_query_spatial(x, y, z, radius, componentFilter, limit)` - Find objects within a sphere/box region, optionally filtered by component
- `smart_align_to_ground(maxDistance, alignRotation)` - Raycast selected objects downward to align them to the ground. Requires objects selected in Hierarchy first.
- `smart_distribute(axis)` - Evenly distribute selected objects between first and last positions. Requires at least 3 objects selected in Hierarchy first.
- `smart_snap_to_grid(gridSize)` - Snap selected objects to a grid
- `smart_randomize_transform(posRange, rotRange, scaleMin, scaleMax)` - Randomize position/rotation/scale of selected objects within ranges
- `smart_replace_objects(prefabPath)` - Replace selected objects with a prefab (preserving transforms). Requires objects selected in Hierarchy first.
- `smart_select_by_component(componentName)` - Select all objects that have a specific component

### Terrain
- `terrain_create(name, width, length, height, heightmapResolution, x, y, z)` - Create a new Terrain with TerrainData asset
- `terrain_get_info(name, instanceId)` - Get terrain information including size, resolution, and layers
- `terrain_get_height(worldX, worldZ, name, instanceId)` - Get terrain height at world position
- `terrain_set_height(normalizedX, normalizedZ, height, name, instanceId)` - Set terrain height at normalized coordinates (0-1)
- `terrain_set_heights_batch(startX, startZ, heights, name, instanceId)` - Set terrain heights in a rectangular region. Heights is a 2D array [z][x] with values 0-1.
- `terrain_add_hill(normalizedX, normalizedZ, radius, height, smoothness, name, instanceId)` - Add a smooth hill to the terrain at normalized position with radius and height
- `terrain_generate_perlin(scale, heightMultiplier, octaves, persistence, lacunarity, seed, name, instanceId)` - Generate terrain using Perlin noise for natural-looking landscapes
- `terrain_smooth(normalizedX, normalizedZ, radius, iterations, name, instanceId)` - Smooth terrain heights in a region to reduce sharp edges
- `terrain_flatten(normalizedX, normalizedZ, targetHeight, radius, strength, name, instanceId)` - Flatten terrain to a specific height in a region
- `terrain_paint_texture(normalizedX, normalizedZ, layerIndex, strength, brushSize, name, instanceId)` - Paint terrain texture layer at normalized position. Requires terrain layers to be set up.

### Test
- `test_run(testMode, filter)` - Run Unity tests asynchronously. Returns a jobId immediately — poll with test_get_result(jobId) to check status.
- `test_get_result(jobId)` - Get the result of a test run. Requires the jobId returned by test_run or test_run_by_name.
- `test_list(testMode, limit)` - List available tests
- `test_cancel(jobId)` - Cancel a running test
- `test_run_by_name(testName, testMode)` - Run specific tests by class or method name
- `test_get_last_result()` - Get the most recent test run result
- `test_list_categories(testMode)` - List test categories
- `test_create_editmode(testName, folder)` - Create an EditMode test script template
- `test_create_playmode(testName, folder)` - Create a PlayMode test script template
- `test_get_summary()` - Get aggregated test summary across all runs

### Texture
- `texture_get_settings(assetPath)` - Get texture import settings for an image asset
- `texture_set_settings(assetPath, textureType, maxSize, filterMode, compression, mipmapEnabled, sRGB, readable, alphaIsTransparency, spritePixelsPerUnit, wrapMode, npotScale)` - Set texture import settings. textureType: Default/NormalMap/Sprite/Editor GUI/Cursor/Cookie/Lightmap/SingleChannel. maxSize: 32-8192. filterMode: Point/Bilinear/Trilinear. compression: None/LowQuality/Normal/HighQuality
- `texture_set_settings_batch(items)` - Set texture import settings for multiple images. items: JSON array of {assetPath, textureType, maxSize, filterMode, ...}
- `texture_find_assets(filter, limit)` - Search for texture assets in the project
- `texture_get_info(assetPath)` - Get detailed texture information (dimensions, format, memory)
- `texture_set_type(assetPath, textureType)` - Set texture type. textureType: Default/NormalMap/Sprite/EditorGUI/Cursor/Cookie/Lightmap/SingleChannel
- `texture_set_platform_settings(assetPath, platform, maxSize, format, compressionQuality, overridden)` - Set platform-specific texture settings. platform: Standalone/iPhone/Android/WebGL
- `texture_get_platform_settings(assetPath, platform)` - Get platform-specific texture settings. platform: Standalone/iPhone/Android/WebGL
- `texture_set_sprite_settings(assetPath, pixelsPerUnit, spriteMode)` - Configure Sprite-specific settings (pixelsPerUnit, spriteMode)
- `texture_find_by_size(minSize, maxSize, limit)` - Find textures by dimension range (minSize/maxSize in pixels)

### Timeline
- `timeline_create(name, folder)` - Create a new Timeline asset and Director instance
- `timeline_add_audio_track(name, instanceId, path, trackName)` - Add an Audio track to a Timeline
- `timeline_add_animation_track(name, instanceId, path, trackName, bindingObjectName)` - Add an Animation track to a Timeline, optionally binding an object
- `timeline_add_activation_track(name, instanceId, path, trackName)` - Add an Activation track to control object visibility
- `timeline_add_control_track(name, instanceId, path, trackName)` - Add a Control track for nested Timelines or prefab spawning
- `timeline_add_signal_track(name, instanceId, path, trackName)` - Add a Signal track for event markers
- `timeline_remove_track(name, instanceId, path, trackName)` - Remove a track by name from a Timeline
- `timeline_list_tracks(name, instanceId, path)` - List all tracks in a Timeline
- `timeline_add_clip(name, instanceId, path, trackName, start, duration)` - Add a clip to a track by track name
- `timeline_set_duration(name, instanceId, path, duration, wrapMode)` - Set Timeline duration and wrap mode
- `timeline_play(name, instanceId, path, action)` - Play, pause, or stop a Timeline (Editor preview)
- `timeline_set_binding(name, instanceId, path, trackName, bindingObjectName)` - Set the binding object for a track

### UI
- `ui_create_canvas(name, renderMode)` - Create a new Canvas
- `ui_create_panel(name, parent, r, g, b, a)` - Create a Panel UI element
- `ui_create_button(name, parent, text, width, height)` - Create a Button UI element
- `ui_create_text(name, parent, text, fontSize, r, g, b)` - Create a Text UI element
- `ui_create_image(name, parent, spritePath, width, height)` - Create an Image UI element
- `ui_create_batch(items)` - Create multiple UI elements (Efficient). items: JSON array of {type, name, parent, text, width, height, ...}
- `ui_create_inputfield(name, parent, placeholder, width, height)` - Create an InputField UI element
- `ui_create_slider(name, parent, minValue, maxValue, value, width, height)` - Create a Slider UI element
- `ui_create_toggle(name, parent, label, isOn)` - Create a Toggle UI element
- `ui_set_text(name, instanceId, path, text)` - Set text content on a UI Text element (supports name/instanceId/path)
- `ui_find_all(uiType, limit)` - Find all UI elements in the scene
- `ui_set_anchor(name, instanceId, path, preset, setPivot)` - Set anchor preset for a UI element (TopLeft, TopCenter, TopRight, MiddleLeft, MiddleCenter, MiddleRight, BottomLeft, BottomCenter, BottomRight, StretchHorizontal, StretchVertical, StretchAll)
- `ui_set_rect(name, instanceId, path, width, height, posX, posY, left, right, top, bottom)` - Set RectTransform size, position, and padding (offsets)
- `ui_layout_children(name, instanceId, path, layoutType, spacing, paddingLeft, paddingRight, paddingTop, paddingBottom, gridColumns, childForceExpandWidth, childForceExpandHeight)` - Arrange child UI elements in a layout (Vertical, Horizontal, Grid)
- `ui_align_selected(alignment)` - Align selected UI elements (Left, Center, Right, Top, Middle, Bottom)
- `ui_distribute_selected(direction)` - Distribute selected UI elements evenly (Horizontal, Vertical)

### Validation
- `validate_scene(checkMissingScripts, checkMissingPrefabs, checkDuplicateNames, checkEmptyGameObjects)` - Validate current scene for common issues
- `validate_find_missing_scripts(searchInPrefabs)` - Find all GameObjects with missing scripts
- `validate_cleanup_empty_folders(rootPath, dryRun)` - Find and optionally delete empty folders
- `validate_find_unused_assets(assetType, limit)` - Find potentially unused assets
- `validate_texture_sizes(maxRecommendedSize, limit)` - Find textures that may need optimization
- `validate_project_structure(rootPath, maxDepth)` - Get overview of project structure
- `validate_fix_missing_scripts(dryRun)` - Remove missing script components from GameObjects
- `validate_missing_references(limit)` - Find null/missing object references on components in the scene
- `validate_mesh_collider_convex(limit)` - Find non-convex MeshColliders (potential performance issue)
- `validate_shader_errors(limit)` - Find shaders with compilation errors

### Workflow
- `bookmark_set(bookmarkName, note)` - Save current selection and scene view position as a bookmark
- `bookmark_goto(bookmarkName)` - Restore selection and scene view from a bookmark
- `bookmark_list()` - List all saved bookmarks
- `bookmark_delete(bookmarkName)` - Delete a bookmark
- `history_undo(steps)` - Undo the last operation (or multiple steps)
- `history_redo(steps)` - Redo the last undone operation (or multiple steps)
- `history_get_current()` - Get the name of the current undo group
- `workflow_task_start(tag, description)` - Start a new persistent workflow task to track changes for undo. Call workflow_task_end when done.
- `workflow_task_end()` - End the current workflow task and save it. Requires an active task (call workflow_task_start first).
- `workflow_snapshot_object(name, instanceId)` - Manually snapshot an object's state before modification. Requires an active task (call workflow_task_start first).
- `workflow_list()` - List persistent workflow history
- `workflow_undo_task(taskId)` - Undo changes from a specific task (restore to previous state)
- `workflow_redo_task(taskId)` - Redo a previously undone task (restore changes)
- `workflow_undone_list()` - List all undone tasks that can be redone
- `workflow_revert_task(taskId)` - Alias for workflow_undo_task (deprecated, use workflow_undo_task instead)
- `workflow_snapshot_created(name, instanceId)` - Record a newly created object for undo tracking. Requires an active task (call workflow_task_start first).
- `workflow_delete_task(taskId)` - Delete a task from history (does not revert changes)
- `workflow_session_start(tag)` - Start a new session (conversation-level). All changes will be tracked and can be undone together.
- `workflow_session_end()` - End the current session and save all tracked changes.
- `workflow_session_undo(sessionId)` - Undo all changes made during a specific session (conversation-level undo)
- `workflow_session_list()` - List all recorded sessions (conversation-level history)
- `workflow_session_status()` - Get the current session status

## Skill Directory Structure

```
unity-skills/
├── SKILL.md          # This file - skill entry point
└── scripts/
    └── unity_skills.py  # Python helper with call_skill(), is_unity_running(), etc.
```

## Direct REST API

```bash
# Health check
curl http://localhost:8090/health

# List all available skills
curl http://localhost:8090/skills

# Execute a skill
curl -X POST http://localhost:8090/skill/gameobject_create \
  -H 'Content-Type: application/json' \
  -d '{"name":"MyCube", "primitiveType":"Cube"}'
```
