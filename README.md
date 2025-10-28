# Catering Management System

A comprehensive desktop application for managing catering business operations, built with WPF (Windows Presentation Foundation) and .NET 8.0.

## 📋 Table of Contents
- [Overview](#overview)
- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Configuration](#configuration)
- [How to Run](#how-to-run)
- [Project Structure](#project-structure)
- [Technologies Used](#technologies-used)
- [License](#license)

## 🎯 Overview

The Catering Management System is a Windows desktop application designed to streamline and automate catering business operations. It provides a complete solution for managing reservations, menu options, workers, equipment, kitchen inventory, and payroll. The application features a modern Material Design UI and integrates with Supabase for cloud-based data storage and real-time updates.

### Purpose

This application helps catering businesses to:
- **Manage Reservations**: Track event bookings with details like celebrant name, venue, date, time, and guest counts
- **Organize Menu Options**: Maintain catalog of menu items, packages, and grazing table options
- **Handle Theme & Motifs**: Configure event themes and decorative motifs
- **Worker Management**: Add, edit, and assign workers to events
- **Equipment Tracking**: Monitor catering equipment inventory
- **Kitchen Inventory**: Manage kitchen items and supplies
- **Payroll System**: Generate payslips and manage worker payments
- **Real-time Dashboard**: View business metrics and reservation statistics
- **Document Generation**: Create PDF contracts and reports

## ✨ Features

### Core Functionality
- **User Authentication**: Secure login and registration system with admin controls
- **Dashboard Analytics**: Real-time counters and visualizations with LiveCharts
- **Reservation Management**: 
  - Create, view, edit, and track reservations
  - Assign packages, menus, themes, and grazing tables to events
  - Monitor reservation status (pending, confirmed, completed)
  - Receipt number generation
- **Menu & Package Management**: Configure food options and service packages
- **Theme & Motif Configuration**: Set up event themes and decorations
- **Worker Scheduling**: Assign workers to specific events and manage schedules
- **Equipment Management**: Track equipment inventory and availability
- **Kitchen Inventory**: Monitor kitchen supplies and items
- **Payroll System**: Generate and manage worker payslips
- **Document Generation**: PDF contract and report generation
- **Email Notifications**: Automated email service integration
- **Cloud Storage**: Supabase backend for data persistence and real-time updates

### User Interface
- Modern Material Design UI
- Responsive and intuitive navigation
- Real-time data updates
- Chart and graph visualizations

## 🔧 Prerequisites

Before running this application, ensure you have the following installed:

1. **Operating System**: Windows 10 or later (Windows application)
2. **.NET SDK**: .NET 8.0 or later
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
3. **Visual Studio** (Recommended):
   - Visual Studio 2022 or later
   - Workload: .NET desktop development
   - Alternative: Visual Studio Code with C# extension
4. **Supabase Account**: 
   - Create a free account at https://supabase.com
   - Set up a new project and obtain API credentials
5. **Firebase Admin** (Optional): 
   - For additional cloud services if needed

## 📥 Installation

### Step 1: Clone the Repository
```bash
git clone https://github.com/Johnravee/CATERINGMANAGEMENT.git
cd CATERINGMANAGEMENT
```

### Step 2: Restore NuGet Packages
Using .NET CLI:
```bash
dotnet restore
```

Or in Visual Studio:
- Right-click on the solution in Solution Explorer
- Select "Restore NuGet Packages"

### Step 3: Verify Dependencies
The application uses the following major packages (automatically installed with restore):
- Material Design Themes (UI Framework)
- Supabase Client (Database)
- LiveCharts (Data Visualization)
- PDFsharp (Document Generation)
- FirebaseAdmin (Authentication)
- DotNetEnv (Environment Variables)

## ⚙️ Configuration

### Environment Variables Setup

1. **Copy the example environment file**:
   ```bash
   cp .env.example .env
   ```

2. **Edit the `.env` file** with your Supabase credentials:
   ```
   SUPABASE_URL=https://your-project.supabase.co
   SUPABASE_API_KEY=your-anon-key-here
   ```

3. **Obtain Supabase Credentials**:
   - Log in to your Supabase dashboard: https://app.supabase.com
   - Select your project
   - Go to Settings → API
   - Copy the "Project URL" and "anon public" key
   - Paste them into your `.env` file

### Database Setup (Supabase)

The application expects the following tables in your Supabase database:
- `reservations` - Event reservations
- `profile` - User profiles
- `thememotif` - Event themes and motifs
- `grazing` - Grazing table options
- `package` - Service packages
- `menu_orders` - Menu order details
- `workers` - Worker information
- `equipment` - Equipment inventory
- `kitchen` - Kitchen items
- `payroll` - Payroll records
- `feedback` - Customer feedback

**Note**: You may need to set up these tables in your Supabase project based on the Models defined in the application.

### Firebase Configuration (Optional)

If using Firebase services:
1. Place your `service-account-file.json` in the `Credentials/` folder
2. Ensure the file is included in your build output (already configured in .csproj)

## 🚀 How to Run

### Method 1: Using Visual Studio (Recommended)

1. **Open the Solution**:
   - Double-click `CATERINGMANAGEMENT.sln` to open in Visual Studio

2. **Set Build Configuration**:
   - Select "Debug" or "Release" from the toolbar
   - Ensure "Any CPU" is selected as the platform

3. **Build the Solution**:
   - Press `Ctrl+Shift+B` or go to Build → Build Solution
   - Wait for the build to complete successfully

4. **Run the Application**:
   - Press `F5` (Start Debugging) or `Ctrl+F5` (Start Without Debugging)
   - The Dashboard window will appear as the startup window

### Method 2: Using .NET CLI

1. **Build the Application**:
   ```bash
   dotnet build
   ```

2. **Run the Application**:
   ```bash
   dotnet run
   ```

### Method 3: Running the Executable

1. **Build Release Version**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **Navigate to Output Directory**:
   ```bash
   cd publish
   ```

3. **Run the Executable**:
   ```bash
   ./CATERINGMANAGEMENT.exe
   ```

## 📁 Project Structure

```
CATERINGMANAGEMENT/
│
├── App.xaml                    # Application entry point and resources
├── App.xaml.cs                 # Application code-behind
├── AssemblyInfo.cs             # Assembly metadata
├── CATERINGMANAGEMENT.csproj   # Project configuration
├── CATERINGMANAGEMENT.sln      # Solution file
│
├── Assets/                     # Application assets
│   └── images/                 # Image resources (logos, icons)
│
├── Converters/                 # Value converters for data binding
│
├── Credentials/                # Authentication credentials
│   └── service-account-file.json  # Firebase service account (not in repo)
│
├── DocumentsGenerator/         # PDF and document generation
│
├── Helpers/                    # Utility and helper classes
│
├── Mailer/                     # Email service implementation
│
├── Models/                     # Data models
│   ├── Reservation.cs          # Reservation model
│   ├── Profile.cs              # User profile model
│   ├── Worker.cs               # Worker model
│   ├── Equipment.cs            # Equipment model
│   ├── Kitchen.cs              # Kitchen inventory model
│   ├── Package.cs              # Service package model
│   ├── MenuOption.cs           # Menu item model
│   ├── ThemeMotif.cs           # Theme model
│   ├── GrazingTable.cs         # Grazing table model
│   ├── Payroll.cs              # Payroll model
│   └── ...                     # Other models
│
├── Services/                   # Business logic services
│   ├── Supabase.cs             # Supabase client service
│   ├── AuthService.cs          # Authentication service
│   ├── EmailService.cs         # Email service
│   ├── SessionService.cs       # Session management
│   ├── Data/                   # Data access services
│   └── Shared/                 # Shared service components
│
├── Templates/                  # Document templates
│
├── View/                       # UI Views
│   └── Windows/                # Window views
│       ├── Dashboard.xaml      # Main dashboard
│       ├── loginform.xaml      # Login window
│       ├── Registration.xaml   # User registration
│       ├── ReservationDetails.xaml  # Reservation details
│       ├── AddWorker.xaml      # Add worker window
│       ├── PayrollWindow.xaml  # Payroll management
│       └── ...                 # Other windows
│
└── ViewModels/                 # View model classes (MVVM pattern)
```

## 🛠️ Technologies Used

### Framework & Language
- **.NET 8.0** - Modern .NET framework
- **C#** - Primary programming language
- **WPF (Windows Presentation Foundation)** - Desktop UI framework
- **XAML** - UI markup language

### UI & Design
- **Material Design Themes** - Modern UI design system
- **LiveCharts** - Data visualization and charts
- **LiveCharts WPF** - WPF integration for charts
- **SkiaSharp** - 2D graphics rendering

### Backend & Database
- **Supabase** - Backend as a Service (BaaS)
- **Supabase Postgrest** - PostgreSQL REST API client
- **Firebase Admin** - Firebase cloud services
- **Google.Apis.Auth** - Google authentication

### Utilities
- **PDFsharp** - PDF document generation
- **DotNetEnv** - Environment variable management
- **Microsoft.Extensions.Caching.Memory** - In-memory caching

### Architecture Pattern
- **MVVM (Model-View-ViewModel)** - Separation of concerns
- **Dependency Injection** - Service management
- **Async/Await** - Asynchronous programming

## 🔐 Security Notes

- Never commit your `.env` file to version control
- Keep your Supabase API keys secure
- Store sensitive credentials in environment variables
- The `.gitignore` file is configured to exclude sensitive files

## 📝 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## 🤝 Support

For issues, questions, or contributions, please contact the repository owner or open an issue on GitHub.

---

**Note**: This application is designed for Windows desktop environments and requires appropriate configuration of cloud services (Supabase) before use.