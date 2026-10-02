using Lecture09.Background.Airflow.UploadService.External.HttpClients.Models;

namespace Lecture09.Background.Airflow.UploadService.External.HttpClients;

public interface IAirflowClient
{
    Task<DagRunDto> TriggerDagRunAsync(string dagId, string dagRunId, object conf);
}
