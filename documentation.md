📄 Project Documentation: Doctor Finder App
🏥 1. Overview
App Name: DocReach (placeholder)
Description: A mobile-first platform that allows users to locate verified doctors near them
during emergencies. Doctors and users can register and complete KYC for safety. Admin
dashboard allows manual doctor approval.
👥 2. User Roles
A. User (Patient)
● Register/Login
● Complete KYC (photo ID, basic info)
● Find nearby doctors (based on specialty, distance, etc.)
● Contact doctor (call/chat)
● View doctor profiles
B. Doctor
● Register/Login
● Upload medical license + hospital affiliation
● Set availability status (Online/Offline)
● Specialties, profile photo, brief bio
● Wait for admin approval before going public
C. Admin
● Login to dashboard
● Review and approve/reject doctor applications
● Manage users and doctors
● View system stats and logs
⚙️ 3. Tech Stack
Layer Tech
Mobile App FlutterFlow
API ASP.NET MVC (REST API)
Database SQL Server / PostgreSQL
Auth ASP.NET Identity
Map
Services
Google Maps API
KYC Service Optional: Smile Identity or Manual
🔐 4. KYC & Doctor Verification
User KYC:
● Full name
● Phone number/email
● Upload valid ID (NIN, Passport, Driver’s License)
● Optional face photo
Doctor KYC:
● Same as user, plus:
● Upload Medical License
● Hospital Affiliation
● Degree/Cert scan
● Admin approval required before profile goes live
🧱 5. Core Features (MVP)
Feature User Doctor Admin
Register/Login ✅ ✅ ✅
KYC Upload ✅ ✅ ✅
Location-Based Doctor
Search
✅ ❌ ❌
Real-time Availability Toggle ❌ ✅ ❌
Doctor Profile View ✅ ✅ ✅
Contact Doctor (Call/Chat) ✅ ✅ ❌
Approve/Reject Doctors ❌ ❌ ✅
🗺 6. Location System
● FlutterFlow app requests location access
● User taps "Find Doctors Near Me"
● Backend API fetches nearby doctors with:
○ status = 'Online'
○ distance < 10km
● Returns doctor list + map pins
🖥 7. Admin Dashboard (ASP.NET MVC)
Pages:
● Login
● Dashboard Overview
● Doctors (List, Approve/Reject)
● Users (List)
● Reports (e.g., activity, logs)
● KYC Submissions
🔌 8. API Endpoints (REST - ASP.NET)
Endpoint Method Auth Description
/api/auth/login POST ❌ Login for all users
/api/auth/register POST ❌ Register user or doctor
/api/users/kyc POST ✅ Upload KYC
/api/doctors/availabl
e
GET ✅ Get doctors nearby
/api/doctors/toggle-s
tatus
POST ✅ Doctor sets Online/Offline
/api/admin/doctors GET ✅ Admin fetches doctor list
/api/admin/approve-do
ctor
POST ✅ Admin approves doctor
📱 9. FlutterFlow Notes
● Connect FlutterFlow to your API via Custom API Integration
● Use Google Maps widget for location features
● Authentication can be done via REST (token-based)
● You can add "Role" fields (User/Doctor) in Firestore or API responses
🧩 10. Next Steps
1. Design API structure in ASP.NET MVC
2. Build database schema (Users, Doctors, KYC, Admins)
3. Start building FlutterFlow UI: Registration, Doctor Search
4. Set up REST API connections in FlutterFlow
5. Launch MVP for testing