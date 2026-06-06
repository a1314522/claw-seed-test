import chromadb
from chromadb.config import Settings
from config import CHROMA_DIR, EMBEDDING_MODEL

class ChromaManager:
    def __init__(self):
        self.client = chromadb.PersistentClient(
            path=str(CHROMA_DIR),
            settings=Settings(anonymized_telemetry=False)
        )
        self.collection = self.client.get_or_create_collection("knowledge_base")
        self._encoder = None
        self._encoder_loaded = False
    
    @property
    def encoder(self):
        if not self._encoder_loaded:
            print("[Chroma] Loading embedding model...")
            try:
                from sentence_transformers import SentenceTransformer
                self._encoder = SentenceTransformer(EMBEDDING_MODEL, trust_remote_code=True)
                print("[Chroma] Model loaded.")
            except Exception as e:
                print(f"[Chroma] Model load failed: {e}")
                self._encoder = None
            self._encoder_loaded = True
        return self._encoder
    
    def add_chunks(self, chunks: list, doc_id: int, metadata: dict):
        if not self.encoder:
            print("[Chroma] ERROR: Embedding model not available")
            return 0
        texts = [c[1] for c in chunks]
        ids = [f"doc{doc_id}_chunk{i}" for i in range(len(chunks))]
        metadatas = [{"doc_id": doc_id, "chunk_index": i, "source": metadata.get("filename", ""), **metadata} for i in range(len(chunks))]
        embeddings = self.encoder.encode(texts, normalize_embeddings=True).tolist()
        self.collection.add(embeddings=embeddings, documents=texts, metadatas=metadatas, ids=ids)
        return len(chunks)
    
    def query(self, question: str, top_k: int = 5, threshold: float = 0.7):
        if not self.encoder:
            return []
        embedding = self.encoder.encode([question], normalize_embeddings=True).tolist()
        results = self.collection.query(query_embeddings=embedding, n_results=top_k, include=["documents", "metadatas", "distances"])
        chunks = []
        for i in range(len(results["ids"][0])):
            distance = results["distances"][0][i]
            similarity = 1 - distance
            if similarity >= threshold:
                chunks.append({"text": results["documents"][0][i], "metadata": results["metadatas"][0][i], "similarity": round(similarity, 3)})
        return chunks
    
    def delete_by_doc(self, doc_id: int):
        self.collection.delete(where={"doc_id": doc_id})

chroma_mgr = ChromaManager()
