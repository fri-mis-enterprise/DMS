# Changelog
All notable changes to this project will be documented in this file.

The format is based on Semantic Versioning.

---

## [v7.2.0] - 2026-09-04
### Added
- Added role-aware dashboard statistics for active documents, monthly uploads, stored pages, and storage usage.
- Added responsive dashboard statistic cards while retaining the DMS tutorial below the dashboard overview.

### Changed
- Redesigned the login page with a dedicated responsive layout and removed the application navbar from the login experience.
- Changed the authenticated home page into a dashboard with updated welcome content and document actions.

### Fixed
- Fixed unauthenticated access to the home page so users are redirected to the redesigned login page instead of seeing a separate login prompt.

---

## [v7.1.4] - 2026-07-27
### Fixed
- Fixed the logs DataTables endpoint so the activity log table can load server-side results without failing anti-forgery validation.

### Changed
- Changed the logs activity fetch flow to align the server-side endpoint with the current `Views/Logs/Index.cshtml` DataTables request behavior.

---

## [v7.1.3] - 2026-07-27
### Added
- Added a dedicated general search page with its own search form and a document-text search option.

### Changed
- Changed the navigation to use a simple `Search` menu link instead of a crowded header search form.
- Changed general search pagination to use next-page detection instead of exact total counts.
- Changed the general search experience so metadata search is the default and document text search is an explicit user choice.

### Fixed
- Fixed general search query behavior to avoid the heaviest default search path for ordinary metadata searches.
- Fixed general search wording so metadata search and document text search are easier to understand.

---

## [v7.1.2] - 2026-05-22
### Fixed
- Fixed file transfer so selecting the same storage path no longer deletes the document in Cloud Storage.

### Changed
- Blocked no-op transfers in the transfer form and server-side validation when the selected destination matches the current storage path.

---

## [v7.1.1] - 2026-05-18
### Added
- Added PostgreSQL search indexes to improve general search performance.
- Added a shared loading overlay partial for DMS views.
### Fixed
- Fixed the general search flow to run filtering, sorting, counting, and paging in the database instead of in memory.
### Changed
- Changed general search to use a reusable global loading spinner for search submission and result navigation.

---

## [v7.1.0] - 2026-04-16
### Added
- Added loading spinners and submission overlays for upload, edit, transfer, trash, and file list actions.
- Added duplicate file validation when replacing a file from the edit module.
### Fixed
- Fixed the edit form to display the original file name as the current file.
### Changed
- Redesigned the upload and edit pages to simplify the file input flow and remove unnecessary UI code.
- Added required validation for upload fields including file, box number, submitted by, and date submitted.
- Updated edit handling so box number, submitted by, and date submitted changes are saved correctly.

---

## [v7.0.1] - 2026-04-16
### Changed
- Modified and refactor the validation of uploaded/replace of file.

### Added
- Added method to capture the actual number of pages. 

---

## [v7.0.0] - 2026-04-13
### Added
- Added PDF content search support using extracted document text.
- Added OCR processing state tracking for file documents.
- Added asynchronous OCR processing through a background worker and Cloud Run Job execution mode.
- Added production OCR deployment configuration for Cloud Run and Cloud Scheduler integration support.

## Changed
- Changed file upload and replace flows to queue OCR instead of processing it inline.
- Changed the production deployment design so the web service stays request-based while OCR runs as a separate job.
- Changed OCR job processing limits to use a 2-hour execution window for backlog handling.

---

## [v6.0.0] - 2026-04-10
### Changed
- Modified and refactor the existing code base.

---

## [v5.1.2] - 2026-04-07
### Fixed
- Fixed the login endpoint to accept not found user.

---

## [v5.1.1] - 2026-03-05
### Added
- Added new feature to tag if the user is active or not.

---

## [v5.1.0] - 2026-03-05
### Added
- Added new feature to check the company access.
- Added new extenstion for removing commas in string.

### Changed
- Modified the edit user to retain the company access.
- Redesign the user interface for the user management module.

---

## [v5.0.0] - 2026-02-12
### Changed
- Upgrade version to .NET10.

---

## [v4.1.8] - 2026-01-27
### Fixed
- Modified the edit user to retain the access folder.

---

## [v4.1.7] - 2026-01-12
### Changed
- Modified the "DateSubmitted" into nullable.

---

## [v4.1.6] - 2026-01-08
### Fixed
- Fixed the sub-category folder not showing on the Download Module

---

## [v4.1.5] - 2025-12-18
### Changed
- Converted the General Search in to server side pagination.
- Added the file name and box number for searching the keyword.

---

## [v4.1.4] - 2025-12-13
### Changed
- Removed the default value for DateSubmitted.
- Redesigned the output of activity report.

---

## [v4.1.3] - 2025-12-12
### Added
- Added new field for box number.

---

## [v4.1.2] - 2025-10-29
### Added
- Added health check endpoint.

---

## [v4.1.1] - 2025-10-29
### Added
- Validation of user role before proceeding to trash module.

---

## [v4.1.0] - 2025-10-01
### Added
- Store deleted files in trash.
- Users can restore or permanently delete files.

---

## [v4.0.0] - 2025-10-01
### Changed
- File details selection list is now dynamic.

---

## [v3.1.1] - 2025-09-20
### Fixed
- Error occurring during file transfer.

---

## [v3.1.0] - 2025-09-17
### Changed
- Updated company, department, category, and sub-category lists.

---

## [v3.0.1] - 2025-09-17
### Added
- Maintenance mode.

---

## [v3.0.0] - 2025-09-10
### Changed
- Migrated to a serverless approach with CI/CD.

---

## [v2.7.0] - 2025-09-08
### Added
- Stopwatch to calculate upload duration.
- Audit trail for login and logout.

---

## [v2.6.0] - 2025-07-29
### Changed
- Refactored source code.

---

## [v2.5.0] - 2025-07-25
### Changed
- Updated logo.
- Modified video tutorial.

---

## [v2.4.0] - 2025-07-19
### Added
- Video tutorial on the homepage.

---

## [v2.3.1] - 2025-05-29
### Changed
- Redesigned layouts for file uploading, uploaded files, and activity logs.

---

## [v2.3.0] - 2025-04-29
### Added
- Modified edit functionality.
- Added feature to upload a new file.

---

## [v2.2.3] - 2025-02-20
### Added
- Retail audit department.

---

## [v2.2.2] - 2025-02-05
### Added
- Breadcrumb navigation in download module.

---

## [v2.2.1] - 2025-01-17
### Added
- Logs for file downloads.
- Additional access permissions for each user.

---

## [v2.1.1] - 2024-10-22
### Changed
- General search link results changed to downloadable format.

---

## [v2.1.0] - 2024-09-07
### Added
- New module to capture activity reports.

---

## [v2.0.7] - 2024-08-10
### Added
- Open access for logs.

---

## [v2.0.6] - 2024-07-27
### Added
- Server-side pagination in uploaded files.

---

## [v2.0.5] - 2024-06-14
### Added
- New category: Weekly Reports.

---

## [v2.0.4] - 2024-04-30
### Added
- Loading spinner.
### Changed
- Minor UI revisions.

---

## [v2.0.3] - 2024-04-15
### Changed
- Redesigned tables.
- Cleaned up DataTable scripts.

---

## [v2.0.2] - 2024-04-04
### Added
- Deletion restriction (only File Custodian can delete files).
### Fixed
- Search bar issue when user clears input.

---

## [v2.0.1] - 2024-03-16
### Fixed
- Transfer process creating unnecessary folders.

---

## [v2.0.0] - 2024-03-15
### Changed
- Upgraded to .NET 8.
- Optimized codebase.

---

## [v1.0.3] - 2024-03-14
### Added
- New category and department.

---

## [v1.0.2] - 2024-02-21
### Changed
- Improved uploading speed (under monitoring).

---

## [v1.0.1] - 2024-02-16
### Added
- New field for file size.

---

## [v1.0.0] - 2024-02-03
### Added
- Initial published version of DMS.

---

## [Unreleased]
### Added
- (Upcoming features)
### Fixed
- (Upcoming fixes)
### Changed
- (Upcoming changes)
