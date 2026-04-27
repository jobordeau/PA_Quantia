from __future__ import annotations

import os
import sys
from pathlib import Path

import pytest

DAGS_FOLDER = Path(__file__).resolve().parent.parent / "dags"
sys.path.insert(0, str(DAGS_FOLDER))


@pytest.fixture(scope="session")
def dag_bag():
    from airflow.models import DagBag

    os.environ.setdefault("AIRFLOW__CORE__DAGS_FOLDER", str(DAGS_FOLDER))
    return DagBag(dag_folder=str(DAGS_FOLDER), include_examples=False)


def test_no_import_errors(dag_bag):
    assert dag_bag.import_errors == {}, (
        f"DAG import failures detected: {dag_bag.import_errors}"
    )


def test_expected_dags_present(dag_bag):
    expected = {"quantia_crypto_ingestion", "quantia_sentiment_pipeline"}
    found = set(dag_bag.dag_ids)
    missing = expected - found
    assert not missing, f"Expected DAGs missing: {missing}"


@pytest.mark.parametrize("dag_id", ["quantia_crypto_ingestion", "quantia_sentiment_pipeline"])
def test_dag_has_tasks(dag_bag, dag_id):
    dag = dag_bag.get_dag(dag_id)
    assert dag is not None, f"DAG {dag_id} not found"
    assert len(dag.tasks) >= 1, f"DAG {dag_id} has no tasks"
