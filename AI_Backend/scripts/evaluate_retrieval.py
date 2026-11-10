import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

from app.services.rag_service import rag_service


def calculate_metrics(ranks: list[int | None]) -> dict:
    total = len(ranks)
    if total == 0:
        return {
            "total": 0,
            "hit_at_1": 0.0,
            "hit_at_k": 0.0,
            "mrr_at_k": 0.0,
        }

    return {
        "total": total,
        "hit_at_1": sum(1 for rank in ranks if rank == 1) / total,
        "hit_at_k": sum(1 for rank in ranks if rank is not None) / total,
        "mrr_at_k": sum(1.0 / rank for rank in ranks if rank is not None) / total,
    }


def run_evaluation(cases: list[dict], k: int, verbose: bool = False) -> dict:
    results = []
    ranks: list[int | None] = []

    for case in cases:
        expected = set(case.get("expected_doc_ids") or [])
        sources = rag_service.query_rag_with_sources(case["query"], n_results=k)
        rank = None
        for index, source in enumerate(sources, start=1):
            if source.get("doc_id") in expected:
                rank = index
                break

        ranks.append(rank)
        results.append(
            {
                "id": case.get("id"),
                "query": case["query"],
                "expected_doc_ids": sorted(expected),
                "rank": rank,
                "retrieved_doc_ids": [source.get("doc_id") for source in sources],
            }
        )
        if verbose:
            status = f"rank={rank}" if rank else "MISS"
            print(f"[{status}] {case.get('id')}: {case['query']}")

    metrics = calculate_metrics(ranks)
    return {"metrics": metrics, "results": results}


def main() -> int:
    parser = argparse.ArgumentParser(description="Đánh giá chất lượng retrieval RAG")
    parser.add_argument(
        "--cases",
        type=Path,
        default=ROOT / "evals" / "retrieval_cases.json",
        help="Đường dẫn file JSON chứa các case đánh giá",
    )
    parser.add_argument("--k", type=int, default=3, help="Số kết quả retrieval mỗi query")
    parser.add_argument("--json-out", type=Path, help="Ghi kết quả đầy đủ ra file JSON")
    parser.add_argument("--verbose", action="store_true", help="In trạng thái từng case")
    args = parser.parse_args()

    if args.k < 1:
        parser.error("--k phải lớn hơn 0")

    payload = json.loads(args.cases.read_text(encoding="utf-8"))
    cases = payload.get("cases") or []
    report = run_evaluation(cases, args.k, args.verbose)

    metrics = report["metrics"]
    print(
        "Cases: {total} | hit@1: {hit_at_1:.3f} | hit@{k}: {hit_at_k:.3f} | MRR@{k}: {mrr_at_k:.3f}".format(
            k=args.k,
            **metrics,
        )
    )

    if args.json_out:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(
            json.dumps(report, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        print(f"Đã ghi kết quả: {args.json_out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
