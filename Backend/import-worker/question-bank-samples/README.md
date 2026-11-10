# Bộ câu hỏi mẫu để nhập ngân hàng

Các tệp `.txt` trong thư mục này là nội dung mẫu do nhóm tự biên soạn, có thể mở rồi sao chép vào Word. Mỗi tệp hiện minh họa cấp độ 1 cho một khóa học; giáo viên có thể sửa, bổ sung câu hỏi và đáp án trước khi tải lên.

## Cách dùng

1. Mở tệp mẫu của khóa học cần dùng và sao chép nội dung vào Word.
2. Giữ mỗi câu hỏi bắt đầu bằng `Câu <số>:` để hệ thống nhận diện ranh giới câu.
3. Ghi đáp án ở dòng riêng, bắt đầu bằng `Đáp án giáo viên:`. Dòng này sẽ được tách khỏi nội dung học sinh nhìn thấy.
4. Lưu thành `.docx` hoặc xuất thành PDF có thể chọn/copy văn bản.
5. Tải lên từ Ngân hàng câu hỏi, kiểm tra bản AI trích xuất và sửa nếu cần, rồi mới lưu.

Không cần mã câu hỏi. Dạng trắc nghiệm, điền khuyết, đúng/sai và tính nhẩm theo nhịp có thể được trộn trong cùng tài liệu. Với tính nhẩm theo nhịp, dùng đúng cấu trúc `Tính nhẩm theo nhịp: 1, +2, -3`; hiện mỗi số hiển thị cố định 1 giây. Ghi riêng đáp án kết quả cuối cùng. PDF scan/ảnh chụp chưa được hỗ trợ OCR và có thể bị từ chối.

## Các dạng câu hỏi

- **Trắc nghiệm (`MULTIPLE_CHOICE`)**: ghi các lựa chọn trên từng dòng, bắt đầu bằng `A.`, `B.`, `C.`, `D.`; đáp án giáo viên bắt đầu bằng chữ cái lựa chọn.
- **Đúng/Sai (`TRUE_FALSE`)**: viết một nhận định rõ ràng; đáp án giáo viên bắt đầu bằng `Đúng` hoặc `Sai`.
- **Điền khuyết / bài toán có một đáp số (`FILL_IN`)**: nội dung có một ô trống hoặc bài toán lời văn; đáp án giáo viên ghi đúng đáp số ở đầu dòng.
- **Tính nhẩm theo nhịp (`TIMED_SEQUENCE`)**: ghi `Tính nhẩm theo nhịp: số đầu, +số, -số`; đáp án giáo viên chỉ ghi kết quả cuối. App hiện lần lượt từng số trong 1 giây rồi cho học sinh nhập kết quả.
- **Tự luận (`ESSAY`)**: AI có thể nhận diện và lưu vào ngân hàng, nhưng luồng bài tập trên app hiện chưa hỗ trợ học sinh nộp/chấm tự luận.

## Tệp mẫu

- `fingermath-level-1.txt`
- `soroban-level-1.txt`
- `logic-level-1.txt`

Khi chạy với `LLM_PROVIDER=gemini` hoặc `openai`, cần cấu hình API key hợp lệ. `LLM_PROVIDER=mock` chỉ dùng để kiểm thử luồng offline, không gọi AI và không nên dùng để chứng minh mô hình AI đang hoạt động.
