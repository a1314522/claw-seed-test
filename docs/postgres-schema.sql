-- OpenClaw PaaS - PostgreSQL Migration Script
-- For AIKnowledgeBase + AssetManagement System
-- Compatible with PostgreSQL 15+

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================
-- System Tables
-- ============================================

-- System Users (Unified user table for both KB and AM)
CREATE TABLE IF NOT EXISTS sys_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255),
    email VARCHAR(100),
    phone VARCHAR(20),
    display_name VARCHAR(100),
    department_id UUID,
    is_active BOOLEAN DEFAULT TRUE,
    is_admin BOOLEAN DEFAULT FALSE,
    ldap_dn VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP
);

-- Departments (Organization structure)
CREATE TABLE IF NOT EXISTS departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    dept_code VARCHAR(50) UNIQUE NOT NULL,
    dept_name VARCHAR(100) NOT NULL,
    parent_id UUID REFERENCES departments(id),
    manager_id UUID,
    level INT DEFAULT 1,
    source VARCHAR(20) DEFAULT 'manual', -- manual, kingdee, ad
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Add foreign key for sys_users.department_id
ALTER TABLE sys_users 
    ADD CONSTRAINT fk_user_department 
    FOREIGN KEY (department_id) REFERENCES departments(id) ON DELETE SET NULL;

-- Roles (RBAC)
CREATE TABLE IF NOT EXISTS sys_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_code VARCHAR(50) UNIQUE NOT NULL,
    role_name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- User-Role mapping
CREATE TABLE IF NOT EXISTS sys_user_roles (
    user_id UUID REFERENCES sys_users(id) ON DELETE CASCADE,
    role_id UUID REFERENCES sys_roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

-- Permissions
CREATE TABLE IF NOT EXISTS sys_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource VARCHAR(50) NOT NULL,
    action VARCHAR(50) NOT NULL,
    description TEXT,
    UNIQUE(resource, action)
);

-- Role-Permission mapping
CREATE TABLE IF NOT EXISTS sys_role_permissions (
    role_id UUID REFERENCES sys_roles(id) ON DELETE CASCADE,
    permission_id UUID REFERENCES sys_permissions(id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

-- ============================================
-- Knowledge Base Tables
-- ============================================

-- Categories
CREATE TABLE IF NOT EXISTS kb_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    parent_id UUID REFERENCES kb_categories(id),
    sort_order INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Documents
CREATE TABLE IF NOT EXISTS kb_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(255) NOT NULL,
    content TEXT,
    category_id UUID REFERENCES kb_categories(id),
    tags VARCHAR[],
    created_by UUID REFERENCES sys_users(id),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

-- Document History (Audit trail)
CREATE TABLE IF NOT EXISTS kb_document_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,
    content_snapshot TEXT,
    performed_by UUID REFERENCES sys_users(id),
    performed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================
-- Asset Management Tables
-- ============================================

-- Assets (Fixed assets and consumables)
CREATE TABLE IF NOT EXISTS am_assets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_code VARCHAR(50) UNIQUE NOT NULL,
    asset_name VARCHAR(255) NOT NULL,
    asset_type VARCHAR(50), -- fixed_asset, consumable, low_value
    category VARCHAR(100),
    department_id UUID REFERENCES departments(id),
    user_id UUID REFERENCES sys_users(id),
    purchase_date DATE,
    purchase_price DECIMAL(15,2),
    vendor VARCHAR(255),
    warranty_period INT, -- months
    status VARCHAR(20) DEFAULT 'in_use', -- in_use, maintenance, scrap, transfer
    location VARCHAR(255),
    specs JSONB,
    scrap_date DATE,
    scrap_reason VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Asset Lifecycle Logs
CREATE TABLE IF NOT EXISTS am_asset_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    asset_id UUID REFERENCES am_assets(id) ON DELETE CASCADE,
    action_type VARCHAR(50) NOT NULL, -- purchase, receive, assign, return, repair, scrap, transfer
    from_user_id UUID REFERENCES sys_users(id),
    to_user_id UUID REFERENCES sys_users(id),
    from_department_id UUID REFERENCES departments(id),
    to_department_id UUID REFERENCES departments(id),
    action_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    remark TEXT,
    operated_by UUID REFERENCES sys_users(id)
);

-- Consumable Usage Records
CREATE TABLE IF NOT EXISTS am_consumable_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    consumable_id UUID REFERENCES am_assets(id),
    user_id UUID REFERENCES sys_users(id),
    department_id UUID REFERENCES departments(id),
    quantity INT NOT NULL,
    usage_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    purpose TEXT,
    approved_by UUID REFERENCES sys_users(id)
);

-- ============================================
-- Indexes for Performance
-- ============================================

CREATE INDEX IF NOT EXISTS idx_kb_documents_category ON kb_documents(category_id);
CREATE INDEX IF NOT EXISTS idx_kb_documents_created_by ON kb_documents(created_by);
CREATE INDEX IF NOT EXISTS idx_kb_documents_is_deleted ON kb_documents(is_deleted);
CREATE INDEX IF NOT EXISTS idx_kb_doc_history_document ON kb_document_history(document_id);

CREATE INDEX IF NOT EXISTS idx_am_assets_code ON am_assets(asset_code);
CREATE INDEX IF NOT EXISTS idx_am_assets_dept ON am_assets(department_id);
CREATE INDEX IF NOT EXISTS idx_am_assets_user ON am_assets(user_id);
CREATE INDEX IF NOT EXISTS idx_am_assets_status ON am_assets(status);
CREATE INDEX IF NOT EXISTS idx_am_assets_type ON am_assets(asset_type);
CREATE INDEX IF NOT EXISTS idx_am_asset_logs_asset ON am_asset_logs(asset_id);
CREATE INDEX IF NOT EXISTS idx_am_asset_logs_date ON am_asset_logs(action_date);
CREATE INDEX IF NOT EXISTS idx_am_consumable_usage_date ON am_consumable_usage(usage_date);

CREATE INDEX IF NOT EXISTS idx_departments_parent ON departments(parent_id);
CREATE INDEX IF NOT EXISTS idx_sys_users_dept ON sys_users(department_id);
CREATE INDEX IF NOT EXISTS idx_sys_user_roles_user ON sys_user_roles(user_id);
CREATE INDEX IF NOT EXISTS idx_sys_role_permissions_role ON sys_role_permissions(role_id);

-- ============================================
-- Seed Data
-- ============================================

-- Default admin user
INSERT INTO sys_users (username, password_hash, email, is_admin, display_name) 
VALUES ('admin', 'admin123', 'admin@example.com', TRUE, 'Administrator')
ON CONFLICT (username) DO NOTHING;

-- Default roles
INSERT INTO sys_roles (role_code, role_name, description) VALUES
('admin', 'System Administrator', 'Full system access'),
('asset_manager', 'Asset Manager', 'Manage assets and inventory'),
('kb_editor', 'Knowledge Editor', 'Create and edit knowledge base content'),
('user', 'Regular User', 'Basic access and read-only')
ON CONFLICT (role_code) DO NOTHING;

-- Default permissions
INSERT INTO sys_permissions (resource, action, description) VALUES
('kb_document', 'create', 'Create knowledge documents'),
('kb_document', 'read', 'Read knowledge documents'),
('kb_document', 'update', 'Update knowledge documents'),
('kb_document', 'delete', 'Delete knowledge documents'),
('am_asset', 'create', 'Create assets'),
('am_asset', 'read', 'Read assets'),
('am_asset', 'update', 'Update assets'),
('am_asset', 'delete', 'Delete assets'),
('am_asset', 'transfer', 'Transfer assets'),
('am_asset', 'scrap', 'Scrap assets'),
('sys_user', 'create', 'Create users'),
('sys_user', 'read', 'Read users'),
('sys_user', 'update', 'Update users'),
('sys_user', 'delete', 'Delete users')
ON CONFLICT (resource, action) DO NOTHING;

-- Assign admin role to admin user
INSERT INTO sys_user_roles (user_id, role_id)
SELECT u.id, r.id
FROM sys_users u, sys_roles r
WHERE u.username = 'admin' AND r.role_code = 'admin'
ON CONFLICT DO NOTHING;

-- Assign all permissions to admin role
INSERT INTO sys_role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM sys_roles r, sys_permissions p
WHERE r.role_code = 'admin'
ON CONFLICT DO NOTHING;

-- ============================================
-- Functions and Triggers
-- ============================================

-- Update updated_at timestamp automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply trigger to tables with updated_at
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tr_kb_documents_updated_at') THEN
        CREATE TRIGGER tr_kb_documents_updated_at
        BEFORE UPDATE ON kb_documents
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'tr_am_assets_updated_at') THEN
        CREATE TRIGGER tr_am_assets_updated_at
        BEFORE UPDATE ON am_assets
        FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    END IF;
END $$;

-- ============================================
-- Views for Reports
-- ============================================

-- Asset summary by department
CREATE OR REPLACE VIEW v_asset_summary_by_dept AS
SELECT 
    d.dept_name,
    COUNT(*) as total_assets,
    SUM(CASE WHEN a.status = 'in_use' THEN 1 ELSE 0 END) as in_use,
    SUM(CASE WHEN a.status = 'maintenance' THEN 1 ELSE 0 END) as maintenance,
    SUM(CASE WHEN a.status = 'scrap' THEN 1 ELSE 0 END) as scrapped,
    SUM(a.purchase_price) as total_value
FROM am_assets a
LEFT JOIN departments d ON a.department_id = d.id
WHERE a.is_deleted IS NULL OR a.is_deleted = FALSE
GROUP BY d.id, d.dept_name;

-- Asset summary by category
CREATE OR REPLACE VIEW v_asset_summary_by_category AS
SELECT 
    category,
    COUNT(*) as total,
    SUM(purchase_price) as total_value,
    AVG(purchase_price) as avg_price
FROM am_assets
WHERE is_deleted IS NULL OR is_deleted = FALSE
GROUP BY category;

-- Print completion message
SELECT 'Database schema initialized successfully' as status;
SELECT 'Tables created: ' || COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = 'public';
