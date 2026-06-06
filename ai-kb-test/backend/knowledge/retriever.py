import re, json, math
from collections import Counter
from config import TOP_K, RETRIEVAL_THRESHOLD
from database.sqlite_db import get_all_chunks, delete_doc_chunks, add_chunk

class SimpleSearcher:
    def __init__(self):
        self.docs = []

    def _tokenize(self, text):
        text = text.lower()
        # English alphanumeric tokens
        tokens = re.findall(r'[a-z0-9]+', text)
        # Chinese characters individually
        chinese = re.findall(r'[\u4e00-\u9fff]', text)
        # Also keep 2-3 character Chinese phrases for better context
        phrases = re.findall(r'[\u4e00-\u9fff]{2,3}', text)
        return tokens + chinese + phrases

    def _jaccard_score(self, query_tokens, doc_tokens):
        if not query_tokens or not doc_tokens:
            return 0.0
        q_set = set(query_tokens)
        d_set = set(doc_tokens)
        inter = len(q_set & d_set)
        union = len(q_set | d_set)
        return inter / union if union > 0 else 0.0

    def _tfidf_score(self, query_tokens, doc_tokens):
        if not query_tokens or not doc_tokens:
            return 0.0
        q_counter = Counter(query_tokens)
        d_counter = Counter(doc_tokens)
        score = 0.0
        for tok, qfreq in q_counter.items():
            df = sum(1 for d in self.docs if tok in d['tokens'])
            if df == 0:
                continue
            idf = math.log(len(self.docs) / df + 1)
            tf = d_counter.get(tok, 0)
            score += qfreq * tf * idf
        return score

    def _score(self, q_tokens, d_tokens):
        tfidf = self._tfidf_score(q_tokens, d_tokens)
        jaccard = self._jaccard_score(q_tokens, d_tokens)
        return tfidf * 0.7 + jaccard * 10.0

    def refresh(self):
        rows = get_all_chunks()
        self.docs = []
        for r in rows:
            tokens = self._tokenize(r['text'])
            self.docs.append({
                'id': r['id'], 'doc_id': r['doc_id'], 'text': r['text'],
                'category_id': r.get('category_id', 1),
                'tokens': tokens,
                'metadata': {'source': f'doc_{r["doc_id"]}'}
            })

    def search(self, question, top_k=5, threshold=0.01, category_id=None):
        if not self.docs:
            self.refresh()
        if not self.docs:
            return []
        q_tokens = self._tokenize(question)
        if not q_tokens:
            return []
        scored = []
        for idx, d in enumerate(self.docs):
            if category_id is not None and d.get('category_id') != category_id:
                continue
            s = self._score(q_tokens, d['tokens'])
            if s > 0:
                scored.append((s, idx, d))
        if not scored:
            return []
        scored.sort(reverse=True)
        max_score = scored[0][0]
        results = []
        for s, idx, d in scored[:top_k]:
            sim = s / max_score if max_score > 0 else 0
            if sim >= threshold:
                results.append({'text': d['text'], 'metadata': d['metadata'], 'similarity': round(sim, 3)})
        return results

searcher = SimpleSearcher()
