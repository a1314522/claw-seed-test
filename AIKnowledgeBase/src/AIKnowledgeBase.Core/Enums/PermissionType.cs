namespace AIKnowledgeBase.Core.Enums;

public enum PermissionType
{
    UserView,
    UserCreate,
    UserEdit,
    UserDelete,
    RoleManage,
    CategoryView,
    CategoryCreate,
    CategoryEdit,
    CategoryDelete,
    DocumentView,
    DocumentUpload,
    DocumentDelete,
    DocumentManage, // Admin override
    SearchAll,    // Search across all categories
    HistoryView,
    HistoryClear,
    SystemManage  // Super admin only
}
