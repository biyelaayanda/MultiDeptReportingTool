namespace MultiDeptReportingTool.Constants
{
    public static class Permissions
    {
        // User Management
        public const string USER_VIEW = "user.view";
        public const string USER_CREATE = "user.create";
        public const string USER_UPDATE = "user.update";
        public const string USER_DELETE = "user.delete";
        public const string USER_MANAGE_ROLES = "user.manage_roles";
        
        // Department Management
        public const string DEPARTMENT_VIEW = "department.view";
        public const string DEPARTMENT_CREATE = "department.create";
        public const string DEPARTMENT_UPDATE = "department.update";
        public const string DEPARTMENT_DELETE = "department.delete";
        public const string DEPARTMENT_VIEW_ALL = "department.view_all";
        
        // Report Management
        public const string REPORT_VIEW = "report.view";
        public const string REPORT_CREATE = "report.create";
        public const string REPORT_UPDATE = "report.update";
        public const string REPORT_DELETE = "report.delete";
        public const string REPORT_APPROVE = "report.approve";
        public const string REPORT_VIEW_ALL = "report.view_all";
        public const string REPORT_EXPORT = "report.export";
        
        // Analytics
        public const string ANALYTICS_VIEW = "analytics.view";
        public const string ANALYTICS_VIEW_ALL = "analytics.view_all";
        public const string ANALYTICS_EXPORT = "analytics.export";
        
        // System Administration
        public const string SYSTEM_ADMIN = "system.admin";
        public const string SYSTEM_AUDIT_VIEW = "system.audit.view";
        public const string SYSTEM_SETTINGS = "system.settings";
        public const string SYSTEM_BACKUP = "system.backup";
        
        // Security
        public const string SECURITY_AUDIT_VIEW = "security.audit.view";
        public const string SECURITY_ALERTS_VIEW = "security.alerts.view";
        public const string SECURITY_ALERTS_MANAGE = "security.alerts.manage";
        public const string SECURITY_PERMISSIONS_MANAGE = "security.permissions.manage";
        
        // Export Operations
        public const string EXPORT_CREATE = "export.create";
        public const string EXPORT_VIEW = "export.view";
        public const string EXPORT_DELETE = "export.delete";
        public const string EXPORT_SCHEDULE = "export.schedule";
    }

    public static class Resources
    {
        public const string USER = "User";
        public const string DEPARTMENT = "Department";
        public const string REPORT = "Report";
        public const string ANALYTICS = "Analytics";
        public const string SYSTEM = "System";
        public const string SECURITY = "Security";
        public const string EXPORT = "Export";
    }

    public static class Actions
    {
        public const string VIEW = "View";
        public const string CREATE = "Create";
        public const string UPDATE = "Update";
        public const string DELETE = "Delete";
        public const string APPROVE = "Approve";
        public const string EXPORT = "Export";
        public const string MANAGE = "Manage";
        public const string ADMIN = "Admin";
    }

    public static class Roles
    {
        public const string ADMIN = "Admin";
        public const string EXECUTIVE = "Executive";
        public const string DEPARTMENT_LEAD = "DepartmentLead";
        public const string STAFF = "Staff";
        public const string VIEWER = "Viewer";
    }

    public static class AlertTypes
    {
        public const string FAILED_LOGIN = "FailedLogin";
        public const string MULTIPLE_FAILED_LOGINS = "MultipleFailedLogins";
        public const string SUSPICIOUS_ACTIVITY = "SuspiciousActivity";
        public const string UNAUTHORIZED_ACCESS = "UnauthorizedAccess";
        public const string DATA_BREACH = "DataBreach";
        public const string PERMISSION_ESCALATION = "PermissionEscalation";
        public const string SYSTEM_ERROR = "SystemError";
        public const string CONFIGURATION_CHANGE = "ConfigurationChange";
    }

    public static class Severities
    {
        public const string LOW = "Low";
        public const string MEDIUM = "Medium";
        public const string HIGH = "High";
        public const string CRITICAL = "Critical";
    }

    public static class AuditActions
    {
        public const string LOGIN = "Login";
        public const string LOGOUT = "Logout";
        public const string CREATE = "Create";
        public const string UPDATE = "Update";
        public const string DELETE = "Delete";
        public const string VIEW = "View";
        public const string EXPORT = "Export";
        public const string PERMISSION_GRANTED = "PermissionGranted";
        public const string PERMISSION_REVOKED = "PermissionRevoked";
        public const string ROLE_ASSIGNED = "RoleAssigned";
        public const string ROLE_REMOVED = "RoleRemoved";
        public const string PASSWORD_CHANGED = "PasswordChanged";
        public const string ACCESS_DENIED = "AccessDenied";
    }
}
