# Ai-Mascot-for-PROG  

## Overview  
The **Ai-Mascot-for-PROG** project is a Windows Presentation Foundation (WPF) desktop application developed in C#.  
The system introduces a virtual mascot that guides the user through the application interface while supporting a structured reporting feature.  
The application combines interactive user engagement with practical reporting functionality, demonstrating the integration of graphical assets, data structures, and service-based logic within a .NET environment.  

## Objectives  
- To provide a user-friendly interface enhanced by a mascot character.  
- To facilitate the reporting of issues by capturing essential details such as location, category, description, and an optional image.  
- To implement custom data structures for the management of reports.  
- To integrate graphical and animated assets for improved user engagement.  

## Features  
- **Mascot Interaction**: An AI-driven mascot (assets located in the `vrikkie/` folder) that serves as a guide within the application.  
- **Reporting System**: A structured interface (`ReportWindow`) that enables the logging of reports.  
- **Custom Data Models**: Encapsulated in the `DataStructures` folder, including models such as `ReportItem`.  
- **Service Layer**: Located in the `Services` folder, responsible for backend logic and handling communication tasks.  
- **Resource Management**: The `Assets` folder contains image files and visual resources used by the application.  

## Technical Requirements  
- **Operating System**: Windows 10 or later  
- **Framework**: .NET 6.0 (or the target framework defined in `PROG7312.csproj`)  
- **Development Environment**: Visual Studio 2022 (with the **.NET Desktop Development** workload installed)  

## Installation and Execution  

### Clone or Download the Repository  

git clone https://github.com/ST10375204/Ai-Mascot-for-PROG/
Alternatively, extract the provided ZIP archive.

### Alternatively, extract the provided ZIP archive.  

### Open the Solution  
- Navigate to the project directory.  
- Open the solution file:  
PROG7312.sln
### Restore Dependencies  
- If prompted, restore NuGet packages via Visual Studio.  

### Build the Project  
- Use `Ctrl+Shift+B` or select **Build > Build Solution**.  

### Run the Application  
- Start with `F5` (**Debug > Start Debugging**)  
- Or `Ctrl+F5` to run without debugging.  

## Project Structure  
Ai-Mascot-for-PROG/

│── App.xaml / App.xaml.cs # Application entry point

│── MainWindow.xaml / .cs # Primary interface with mascot

│── ReportWindow.xaml / .cs # Secondary window for reporting

│── DataStructures/ # Custom data models (e.g. ReportItem)

│── Services/ # Service logic and AI interaction

│── Assets/ # Images and visual resources

│── vrikkie/ # Mascot assets

│── PROG7312.sln # Visual Studio solution file

│── PROG7312.csproj # Project configuration file


## Usage Guidelines  
1. Launch the application.  
2. Interact with the mascot displayed in the main window.  
3. To submit a report, navigate to the reporting feature where the following details may be captured:  
   - **Location** of the issue  
   - **Category** of the report  
   - **Description** of the problem  
   - **Image** attachment (optional)  
4. The entered information is stored in structured form for further processing or analysis.  
