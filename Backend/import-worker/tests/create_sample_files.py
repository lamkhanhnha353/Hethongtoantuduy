import io
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import docx
from docx.shared import Inches

if sys.stdout.encoding != 'utf-8':
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except Exception:
        pass


def create_sample_image(output_path: Path):
    """Tạo một ảnh mẫu minh họa hình học (hình vuông và tam giác) cho đề toán."""
    img = Image.new("RGB", (300, 200), color=(245, 247, 250))
    d = ImageDraw.Draw(img)
    # Vẽ hình chữ nhật / hình vuông
    d.rectangle([40, 40, 160, 160], outline=(41, 128, 185), width=3, fill=(214, 234, 248))
    # Vẽ đường chéo
    d.line([40, 40, 160, 160], fill=(231, 76, 60), width=2)
    # Chú thích
    d.text((50, 20), "Hinh vuong ABCD (Canh 9cm)", fill=(44, 62, 80))
    img.save(str(output_path))


def generate_sample_docx(docx_path: Path, img_path: Path):
    """Tạo một file đề thi mẫu .docx chứa cả trắc nghiệm có ảnh, điền số và tự luận."""
    doc = docx.Document()
    doc.add_heading("ĐỀ KIỂM TRA ĐÁNH GIÁ NĂNG LỰC TOÁN TƯ DUY", level=1)
    doc.add_paragraph("Thời gian làm bài: 45 phút - Ngân hàng câu hỏi số: 1\n")

    # Câu 1: Trắc nghiệm có hình ảnh nhúng
    doc.add_paragraph("Câu 1: Cho hình vuông ABCD như hình vẽ bên dưới. Biết chu vi của hình vuông là 36 cm. Hãy tính diện tích của hình vuông ABCD?")
    doc.add_picture(str(img_path), width=Inches(3.0))
    doc.add_paragraph("A. 81 cm2\nB. 36 cm2\nC. 72 cm2\nD. 18 cm2")
    doc.add_paragraph("Đáp án: A. 81 cm2. Lời giải: Cạnh của hình vuông là 36 : 4 = 9 (cm). Diện tích hình vuông là 9 x 9 = 81 (cm2).")

    # Câu 2: Điền số (FILL_IN)
    doc.add_paragraph("Câu 2: Bạn An có một số viên bi. Sau khi An cho Bình một nửa số bi và thêm 2 viên nữa thì An còn lại 6 viên bi. Hỏi lúc đầu An có bao nhiêu viên bi?")
    doc.add_paragraph("Đáp số: 16 viên bi. Lời giải: Một nửa số bi của An là 6 + 2 = 8 viên. Lúc đầu An có 8 x 2 = 16 viên bi.")

    # Câu 3: Tự luận tư duy nâng cao (ESSAY, HARD)
    doc.add_paragraph("Câu 3: Trong một cuộc thi cờ vua vòng tròn một lượt, có 8 kỳ thủ tham gia. Mỗi người chơi đúng một trận với tất cả những người còn lại. Mỗi ván thắng được 1 điểm, hòa được 0.5 điểm, thua được 0 điểm. Chứng minh rằng tổng điểm của tất cả các kỳ thủ sau giải đấu luôn là 28 điểm.")
    doc.add_paragraph("Lời giải: Tổng số trận đấu trong giải là (8 x 7) : 2 = 28 trận. Mỗi trận đấu dù thắng-thua (1 + 0 = 1) hay hòa (0.5 + 0.5 = 1) thì tổng điểm của 2 kỳ thủ luôn là 1. Do đó tổng điểm của 8 kỳ thủ luôn bằng tổng số trận đấu = 28 điểm.")

    doc.save(str(docx_path))
    print(f"[OK] Đã tạo file Word đề thi mẫu tại: {docx_path}")


if __name__ == "__main__":
    test_dir = Path("./tests/data")
    test_dir.mkdir(parents=True, exist_ok=True)

    img_file = test_dir / "sample_geometry.png"
    docx_file = test_dir / "de_thi_toan_tu_duy.docx"

    create_sample_image(img_file)
    generate_sample_docx(docx_file, img_file)
