# XR Accessibility

## Overview
This Unity project provides a comprehensive **Caption System** for VR experiences, designed to enhance accessibility by automatically detecting and displaying synchronized captions for both audio and video content. The system supports real-time caption display with multiple presentation modes (TV-style, character-attached, HMD-based) and a full transcript viewer.

**Unity Version:** 2022.3.62f3  
**Render Pipeline:** Built-in  
**VR SDK:** XR Interaction Toolkit 2.6.4  
**Target Platform(s):** Meta Quest headsets
**Location:** `Assets/CaptionSystem/`

### Key Capabilities
- Automatic detection and captioning of audio and video content
- Real-time synchronization with media playback
- Multiple caption UI modes (fixed, anchored, HMD-following)
- Full transcript viewer with timestamp navigation

### Use Cases
- Accessibility for hard-of-hearing users
- Narrative-driven VR experiences
- Tutorial and educational content

### Learn More
- Watch the [demo video](https://youtu.be/qiRdbVUyIlg) or read this [blog post](https://www.library.rochester.edu/studio-x/projects/xr-caption-toolkit)
- Download the unity package from [this link](#)
- Documentation [website](https://rcl-studio-x.github.io/XR-Accessibility/index.html)

---

## Quick Start

### Prerequisites
- Unity 2022.3.62f3 with the following modules:
  - Android Build Support (for Quest)


### Setup Instructions
1. Clone the repository / Download the unity package from "Release"
2. Open the project in Unity Hub / import the package to your project
3. Open and check demo scene for reference: `Assets/CaptionSystem/_Scene/CaptionSample.unity` (Make sure **Kawaii Slimes** - Character assets is in the project, if not, download from unity asset store)
4. Add the **CaptionSetup** prefab to your own scene (located in `Assets/CaptionSystem/_Prefabs/`)
5. Configure the Caption Database asset (see Caption System Setup below)

---

## Caption System

### Quick Setup Guide

#### 1. Setup the Caption System
1. Drag the `CaptionSetup.prefab` from `Assets/CaptionSystem/_Prefabs/` into your scene
2. Locate the **CaptionDatabase1** asset in `Assets/CaptionSystem/_Scripts/Caption/`
3. In the **CaptionSetup** prefab Inspector, assign the **CaptionDatabase1** to the "Default Database" field

#### 2. Configure Media in Database
1. Select the **CaptionDatabase1** asset in the Project window
2. In the Inspector, expand "Caption Entries"
3. For each media file you want to caption:
   - Click "+" to add a new entry
   - Assign your **AudioClip** or **VideoClip**
   - Assign the corresponding **SRT File** (text asset)
   - Optionally assign a **Caption Prefab** (or use the default)

#### 3. Prepare Your Media Files
- **Audio**
- **Video**
- **SRT Files**: Import .srt caption files as text assets (rename extension to .txt if needed)

#### 4. Add Caption-Enabled Components
**For Audio:**
- Add `CaptionEnabledAudioSource` component to any GameObject with an AudioSource
- The component will automatically register with the GlobalCaptionManager

**For Video:**
- Add `CaptionEnabledVideoPlayer` component to any GameObject with a VideoPlayer
- The component will automatically register with the GlobalCaptionManager

#### 5. Test Your Setup
- Press Play in Unity
- When audio/video plays, captions should appear automatically

### Architecture

**GlobalCaptionManager**
- Singleton manager that handles all caption operations
- Auto-discovers AudioSource and VideoPlayer components in the scene
- Manages caption session lifecycle (start, update, stop)
- Synchronizes caption timing with media playback
- Location: `Assets/CaptionSystem/_Scripts/Caption/GlobalCaptionManager.cs`

**CaptionDatabase (ScriptableObject)**
- Central database linking media files to SRT captions
- Fast lookup system for caption retrieval
- Supports both audio and video clips
- Can be created via: `Right-click → Create → Caption Toolkit → Caption Database`
- Location: `Assets/CaptionSystem/_Scripts/Caption/CaptionDatabase.cs`

**SRTParser**
- Parses SubRip (.srt) format files
- Converts timestamp and text data into CaptionEntry objects
- Handles various SRT formatting variations
- Location: `Assets/CaptionSystem/_Scripts/Caption/SRTParser.cs`

**CaptionEnabledAudioSource**
- Component that makes AudioSource caption-aware
- Automatically registers/unregisters with GlobalCaptionManager
- Supports manual caption override and database override
- Location: `Assets/CaptionSystem/_Scripts/Caption/CaptionEnabledAudioSource.cs`

**CaptionEnabledVideoPlayer**
- Component that makes VideoPlayer caption-aware
- Similar functionality to CaptionEnabledAudioSource but for video
- Location: `Assets/CaptionSystem/_Scripts/Caption/CaptionEnabledVideoPlayer.cs`

---

## Dependencies
- **XR Interaction Toolkit** - Core VR interaction system
- **XR Plugin Management** - VR SDK integration
- **TextMeshPro** - Rich text rendering for captions
- **Unity Video Player** - Video playback support
- **Kawaii Slimes** - Character assets (from Asset Store)
- Video samples in demo downloaded from [Speechpad](https://www.speechpad.com/captions/srt)

---

## Contributors
- Yvie Zhang

