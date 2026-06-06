import sqlite3, json, os, re, hashlib
from datetime import datetime
from config import DATA_DIR, DOC_DIR

DB_PATH = DATA_DIR / "app.db"

def init_db():
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute('''CREATE TABLE IF NOT EXISTS users (
        id INTEGER PRIMARY KEY, username TEXT UNIQUE, password_hash TEXT, is_admin INTEGER DEFAULT 0,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)''')
    c.execute('''CREATE TABLE IF NOT EXISTS categories (
        id INTEGER PRIMARY KEY, name TEXT NOT NULL, description TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)''')
    c.execute('''CREATE TABLE IF NOT EXISTS documents (
        id INTEGER PRIMARY KEY, filename TEXT, original_name TEXT, file_size INTEGER, doc_type TEXT,
        category_id INTEGER DEFAULT 1, chunk_count INTEGER DEFAULT 0, status TEXT DEFAULT 'pending', uploaded_by TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
        FOREIGN KEY (category_id) REFERENCES categories(id))''')
    c.execute('''CREATE TABLE IF NOT EXISTS chunks (
        id INTEGER PRIMARY KEY, doc_id INTEGER, chunk_index INTEGER, text TEXT,
        embedding TEXT, created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)''')
    c.execute('''CREATE TABLE IF NOT EXISTS search_history (
        id INTEGER PRIMARY KEY, user_id INTEGER, question TEXT, answer TEXT, sources TEXT,
        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP)''')
    conn.commit()
    # Ensure default category exists
    c.execute("SELECT id FROM categories WHERE id=1")
    if not c.fetchone():
        c.execute("INSERT INTO categories (id, name, description) VALUES (1, '默认分类', '未分类文档')")
        conn.commit()
    conn.close()

# Migration: add category_id if missing
def migrate_category():
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    try:
        c.execute("SELECT category_id FROM documents LIMIT 1")
    except:
        c.execute("ALTER TABLE documents ADD COLUMN category_id INTEGER DEFAULT 1")
        conn.commit()
    conn.close()

migrate_category()

def get_user(username):
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT * FROM users WHERE username = ?", (username,))
    row = c.fetchone()
    conn.close()
    return dict(row) if row else None

def create_user(username, password_hash, is_admin=False):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("INSERT INTO users (username, password_hash, is_admin) VALUES (?, ?, ?)",
              (username, password_hash, 1 if is_admin else 0))
    user_id = c.lastrowid
    conn.commit()
    conn.close()
    return user_id

def list_users():
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT id, username, is_admin, created_at FROM users ORDER BY created_at DESC")
    rows = [dict(r) for r in c.fetchall()]
    conn.close()
    return rows

def delete_user(user_id):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("DELETE FROM users WHERE id = ?", (user_id,))
    conn.commit()
    conn.close()

def add_document(filename, original_name, file_size, doc_type, uploaded_by, category_id=1):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("INSERT INTO documents (filename, original_name, file_size, doc_type, category_id, uploaded_by) VALUES (?, ?, ?, ?, ?, ?)",
              (filename, original_name, file_size, doc_type, category_id, uploaded_by))
    doc_id = c.lastrowid
    conn.commit()
    conn.close()
    return doc_id

def list_documents(category_id=None):
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    if category_id:
        c.execute("SELECT d.*, c.name as category_name FROM documents d LEFT JOIN categories c ON d.category_id = c.id WHERE d.category_id = ? ORDER BY d.created_at DESC", (category_id,))
    else:
        c.execute("SELECT d.*, c.name as category_name FROM documents d LEFT JOIN categories c ON d.category_id = c.id ORDER BY d.created_at DESC")
    rows = [dict(r) for r in c.fetchall()]
    conn.close()
    return rows

def update_doc_category(doc_id, category_id):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("UPDATE documents SET category_id = ? WHERE id = ?", (category_id, doc_id))
    conn.commit()
    conn.close()

# --- Categories CRUD ---
def create_category(name, description=""):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("INSERT INTO categories (name, description) VALUES (?, ?)", (name, description))
    cat_id = c.lastrowid
    conn.commit()
    conn.close()
    return cat_id

def list_categories():
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT * FROM categories ORDER BY id")
    rows = [dict(r) for r in c.fetchall()]
    conn.close()
    return rows

def get_category(cat_id):
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT * FROM categories WHERE id = ?", (cat_id,))
    row = c.fetchone()
    conn.close()
    return dict(row) if row else None

def delete_category(cat_id):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    # Move docs to default category
    c.execute("UPDATE documents SET category_id = 1 WHERE category_id = ?", (cat_id,))
    c.execute("DELETE FROM categories WHERE id = ?", (cat_id,))
    conn.commit()
    conn.close()

def update_category(cat_id, name, description):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("UPDATE categories SET name = ?, description = ? WHERE id = ?", (name, description, cat_id))
    conn.commit()
    conn.close()

def update_doc_status(doc_id, status, chunk_count=None):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    if chunk_count is not None:
        c.execute("UPDATE documents SET status = ?, chunk_count = ? WHERE id = ?", (status, chunk_count, doc_id))
    else:
        c.execute("UPDATE documents SET status = ? WHERE id = ?", (status, doc_id))
    conn.commit()
    conn.close()

def add_chunk(doc_id, chunk_index, text, embedding=None):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("INSERT INTO chunks (doc_id, chunk_index, text, embedding) VALUES (?, ?, ?, ?)",
              (doc_id, chunk_index, text, json.dumps(embedding) if embedding else None))
    conn.commit()
    conn.close()

def get_all_chunks():
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("""SELECT c.id, c.doc_id, c.chunk_index, c.text, d.category_id 
                 FROM chunks c LEFT JOIN documents d ON c.doc_id = d.id""")
    rows = [dict(r) for r in c.fetchall()]
    conn.close()
    return rows

def delete_doc_chunks(doc_id):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("DELETE FROM chunks WHERE doc_id = ?", (doc_id,))
    conn.commit()
    conn.close()

# --- Search History ---
def add_search_history(user_id, question, answer, sources):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("INSERT INTO search_history (user_id, question, answer, sources) VALUES (?, ?, ?, ?)",
              (user_id, question, answer, json.dumps(sources)))
    conn.commit()
    conn.close()

def get_search_history(user_id, limit=10):
    conn = sqlite3.connect(str(DB_PATH))
    conn.row_factory = sqlite3.Row
    c = conn.cursor()
    c.execute("SELECT id, question, answer, sources, created_at FROM search_history WHERE user_id = ? ORDER BY created_at DESC LIMIT ?", (user_id, limit))
    rows = [dict(r) for r in c.fetchall()]
    conn.close()
    for r in rows:
        try:
            r['sources'] = json.loads(r['sources']) if r['sources'] else []
        except:
            r['sources'] = []
    return rows

def clear_search_history(user_id):
    conn = sqlite3.connect(str(DB_PATH))
    c = conn.cursor()
    c.execute("DELETE FROM search_history WHERE user_id = ?", (user_id,))
    conn.commit()
    conn.close()
