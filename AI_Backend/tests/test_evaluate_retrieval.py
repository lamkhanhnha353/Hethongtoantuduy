import unittest

from scripts.evaluate_retrieval import calculate_metrics


class EvaluateRetrievalTests(unittest.TestCase):
    def test_calculate_metrics(self):
        metrics = calculate_metrics([1, 2, None, 3])

        self.assertEqual(metrics["total"], 4)
        self.assertAlmostEqual(metrics["hit_at_1"], 0.25)
        self.assertAlmostEqual(metrics["hit_at_k"], 0.75)
        self.assertAlmostEqual(metrics["mrr_at_k"], (1 + 0.5 + 1 / 3) / 4)

    def test_calculate_metrics_with_empty_ranks(self):
        metrics = calculate_metrics([])

        self.assertEqual(metrics["total"], 0)
        self.assertEqual(metrics["hit_at_1"], 0.0)
        self.assertEqual(metrics["hit_at_k"], 0.0)
        self.assertEqual(metrics["mrr_at_k"], 0.0)


if __name__ == "__main__":
    unittest.main()
