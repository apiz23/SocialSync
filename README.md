# SocialSync Web

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Bootstrap](https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)

**SocialSync Web** is the browser-based portal for the SocialSync platform. Built with **ASP.NET Core MVC**, it provides a responsive and modern interface for users to access their social dashboard, manage profiles, and interact with the community.

🔗 **Repository:** [https://github.com/apiz23/SocialSync](https://github.com/apiz23/SocialSync)

## ✨ Features

* **💻 Modern UI:** Responsive design using Bootstrap 5 with custom CSS gradients and animations.
* **🔐 Authentication:** Secure Login and Registration pages with client-side validation.
* **⚡ Dashboard:** Interactive feed and user management.
* **🎨 Styling:** Custom styling (`site.css`) featuring glassmorphism effects and smooth transitions.
* **📱 Mobile Responsive:** Optimized for both desktop and mobile browsers.

## 🛠️ Tech Stack

* **Framework:** ASP.NET Core MVC
* **Language:** C#
* **Frontend:** HTML5, Razor Pages, Bootstrap 5, JavaScript/jQuery
* **Icons:** FontAwesome & Bootstrap Icons
* **Backend Connection:** Supabase (Shared with Mobile App)

## 🚀 Getting Started

### Prerequisites
* .NET 7.0 or 8.0 SDK.
* Visual Studio 2022 or VS Code.

### Installation

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/apiz23/SocialSync.git](https://github.com/apiz23/SocialSync.git)
    ```

2.  **Configuration:**
    Open `appsettings.json` and configure your database connection string or API keys:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Your_Connection_String_Here"
    }
    ```

3.  **Run the Application:**
    ```bash
    dotnet run
    ```
    Or press **F5** in Visual Studio.

4.  **Access:**
    Open your browser and navigate to `https://localhost:7000` (or the port specified in your launch profile).

## 📂 Project Structure

* **Controllers/**: Handles incoming browser requests and application logic.
* **Views/**: Razor (`.cshtml`) files for the UI (Login, Dashboard, Home).
* **Models/**: Data structures representing Users and Application state.
* **wwwroot/**: Static assets (CSS, JS, Images, Icons).

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

[MIT](https://choosealicense.com/licenses/mit/)
