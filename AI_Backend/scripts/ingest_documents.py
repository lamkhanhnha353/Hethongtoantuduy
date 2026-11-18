import os
import sys
import shutil
from pathlib import Path

# Thêm đường dẫn gốc của AI_Backend vào sys.path
ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from app.config import settings
from app.services.rag_service import rag_service

CHROMA_DIR = Path(settings.CHROMA_PERSIST_DIRECTORY)
BACKUP_DIR = CHROMA_DIR.parent / "chroma_backup"


def backup_chroma():
    """Tự động sao lưu file chroma.sqlite3 sang chroma_backup."""
    try:
        BACKUP_DIR.mkdir(parents=True, exist_ok=True)
        src_file = CHROMA_DIR / "chroma.sqlite3"
        dst_file = BACKUP_DIR / "chroma.sqlite3"
        if src_file.exists():
            shutil.copy2(src_file, dst_file)
            print(f"[BACKUP] Đã sao lưu ChromaDB an toàn sang: {BACKUP_DIR.name}/chroma.sqlite3")
    except Exception as exc:
        print(f"[CẢNH BÁO] Không thể tạo backup file ({exc}), bỏ qua.")


def list_documents():
    """Liệt kê tất cả các tài liệu hiện có trong ChromaDB."""
    data = rag_service.collection.get(include=["metadatas"])
    metadatas = data.get("metadatas") or []
    doc_stats = {}
    for m in metadatas:
        d_id = str(m.get("doc_id", "unknown"))
        title = str(m.get("title", d_id))
        if d_id not in doc_stats:
            doc_stats[d_id] = {"title": title, "chunks": 0}
        doc_stats[d_id]["chunks"] += 1

    print("\n" + "=" * 65)
    print(f"DANH SÁCH TÀI LIỆU TRONG KHO TRI THỨC ({len(doc_stats)} tài liệu, {len(metadatas)} chunks):")
    print("=" * 65)
    for i, (d_id, info) in enumerate(sorted(doc_stats.items()), 1):
        print(f"{i:2d}. [{d_id}] ({info['chunks']} chunks)")
        print(f"    Tiêu đề: {info['title']}")
    print("=" * 65 + "\n")


def delete_document(doc_id: str):
    """Xóa một tài liệu cụ thể khỏi ChromaDB theo doc_id."""
    count_before = rag_service.collection.count()
    rag_service.collection.delete(where={"doc_id": doc_id})
    count_after = rag_service.collection.count()
    deleted = count_before - count_after
    if deleted > 0:
        print(f"[THÀNH CÔNG] Đã xóa tài liệu '{doc_id}' ({deleted} chunks bị xóa).")
        print(f"Tổng số chunks còn lại trong ChromaDB: {count_after}")
    else:
        print(f"[THÔNG BÁO] Không tìm thấy tài liệu có mã '{doc_id}' trong ChromaDB.")


def ingest_file(file_path: Path):
    """Đọc file tài liệu, cắt đoạn và nạp vào ChromaDB."""
    if not file_path.exists():
        print(f"[LỖI] Không tìm thấy file: {file_path}")
        return

    content = file_path.read_text(encoding="utf-8")

    # Đặt doc_id: nếu file nằm trong thư mục con, lấy luôn tên thư mục làm doc_id
    if file_path.parent.name not in {"documents", "storage"}:
        doc_id = file_path.parent.name
    else:
        doc_id = file_path.stem

    # Lấy dòng tiêu đề đầu tiên nếu có
    lines = [line.strip() for line in content.splitlines() if line.strip()]
    title = lines[0].lstrip("#").strip() if lines else doc_id

    total_before = rag_service.collection.count()
    print(f"\n-> Đang xử lý tài liệu: '{title}' (Mã doc_id: {doc_id})")
    print(f"   Độ dài nội dung: {len(content)} ký tự")
    print(f"   Tổng số chunks trong ChromaDB trước khi nạp: {total_before}")

    result = rag_service.ingest_text_document(
        doc_id=doc_id,
        title=title,
        content=content,
    )

    total_after = rag_service.collection.count()
    print(f"[THÀNH CÔNG] Đã nạp thành công tài liệu vào kho tri thức AI!")
    print(f"   - Số đoạn (chunks) được tạo: {result.get('total_chunks_ingested', 0)}")
    print(f"   - Tổng số chunks hiện tại trong ChromaDB: {total_after} chunks (Tăng +{total_after - total_before})")


def main():
    print("=" * 65)
    print("   HỆ THỐNG NẠP TRI THỨC TOÁN TƯ DUY VÀO CHROMADB (RAG INGEST)")
    print("=" * 65)

    if len(sys.argv) > 1:
        cmd = sys.argv[1].lower()
        if cmd == "--list":
            list_documents()
            return 0
        if cmd == "--delete" and len(sys.argv) > 2:
            delete_document(sys.argv[2])
            return 0
        if cmd == "--rollback":
            target_id = sys.argv[2] if len(sys.argv) > 2 else "thong-tin-trung-tam-va-huong-dan-phu-huynh"
            delete_document(target_id)
            return 0

    # 1. Tự động sao lưu trước khi nạp
    backup_chroma()

    # 2. Xác định file cần nạp
    if len(sys.argv) > 1:
        target_path = Path(sys.argv[1])
        if not target_path.is_absolute():
            target_path = ROOT / target_path
    else:
        # Mặc định nạp file thông tin trung tâm & hướng dẫn phụ huynh
        target_path = (
            ROOT
            / "storage"
            / "documents"
            / "thong-tin-trung-tam-va-huong-dan-phu-huynh"
            / "thong-tin-trung-tam-va-huong-dan-phu-huynh.md"
        )

    if target_path.is_file():
        ingest_file(target_path)
    elif target_path.is_dir():
        md_files = list(target_path.glob("**/*.md")) + list(target_path.glob("**/*.txt"))
        print(f"Tìm thấy {len(md_files)} tệp tài liệu trong thư mục {target_path}")
        for f in md_files:
            ingest_file(f)
    else:
        print(f"[LỖI] Đường dẫn không hợp lệ: {target_path}")
        return 1

    print("\n" + "=" * 65)
    print("HOÀN TẤT! AI Chatbox đã có thể trả lời các câu hỏi về tài liệu này.")
    print("- Xem danh sách: python scripts/ingest_documents.py --list")
    print("- Hoàn tác (xóa tài liệu này): python scripts/ingest_documents.py --rollback")
    print("=" * 65)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
