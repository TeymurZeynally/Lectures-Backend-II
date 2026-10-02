namespace Lecture09.Background.Airflow.UploadService.External.HttpClients.Models;

public class DagRunDto
{
    public string DagRunId { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
