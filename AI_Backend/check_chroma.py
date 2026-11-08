import chromadb

from app.config import settings

client = chromadb.PersistentClient(
    path=settings.CHROMA_PERSIST_DIRECTORY,
    settings=chromadb.Settings(anonymized_telemetry=False),
)

try:
    collection = client.get_collection(name="knowledge_base")
except Exception:
    print("Collection knowledge_base chưa tồn tại.")
    raise SystemExit(0)

all_data = collection.get()

print(f"Tổng số chunks đã lưu trong ChromaDB: {len(all_data['ids'])}")
print("-" * 50)

for idx, doc_id in enumerate(all_data["ids"]):
    print(f"[{idx + 1}] ID: {doc_id}")
    print(f"    - Nội dung: {all_data['documents'][idx]}")
    print(f"    - Metadata: {all_data['metadatas'][idx]}")
    print("-" * 30)
