namespace JobForFresher.Models;
public record ReadinessCheck(string Name, string Status, string Detail);
public class ReadinessReport
{
    public List<ReadinessCheck> Checks { get; } = new();
}
