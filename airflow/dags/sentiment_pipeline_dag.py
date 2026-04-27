from __future__ import annotations

from datetime import datetime, timedelta, timezone

from airflow.decorators import dag
from airflow.models import Variable
from airflow.providers.docker.operators.docker import DockerOperator
from docker.types import Mount


@dag(
    dag_id="quantia_sentiment_pipeline",
    description="Daily sentiment analysis pipeline (Reddit + Google Trends + market composite)",
    schedule="0 1 * * *",
    start_date=datetime(2025, 6, 14, tzinfo=timezone.utc),
    catchup=False,
    max_active_runs=1,
    default_args={
        "owner": "quantia",
        "retries": 1,
        "retry_delay": timedelta(minutes=10),
    },
    tags=["quantia", "sentiment", "nlp"],
)
def sentiment_pipeline_dag():
    DockerOperator(
        task_id="run_sentiment_job",
        image=Variable.get("SENTIMENT_JOB_IMAGE", default_var="quantia/sentiment-job:latest"),
        api_version="auto",
        auto_remove="success",
        environment={
            "PG_CONN":              Variable.get("PG_CONN"),
            "REDDIT_CLIENT_ID":     Variable.get("REDDIT_CLIENT_ID"),
            "REDDIT_CLIENT_SECRET": Variable.get("REDDIT_CLIENT_SECRET"),
            "USER_AGENT":           Variable.get("USER_AGENT", default_var="quantia-sentiment-bot/0.1.0"),
            "OPENAI_API_KEY":       Variable.get("OPENAI_API_KEY"),
            "ASSET":                Variable.get("ASSET", default_var="bitcoin"),
            "LOG_LEVEL":            Variable.get("LOG_LEVEL", default_var="INFO"),
        },
        mounts=[
            Mount(source="/mnt/airflow/logs/sentiment", target="/app/logs", type="bind"),
        ],
        mount_tmp_dir=False,
        network_mode="host",
        docker_url="unix://var/run/docker.sock",
        retrieve_output=False,
    )


sentiment_pipeline_dag()
