namespace Extensions
{
    public static class ExtendHasDataSpecification
    {
        public static IHasDataSpecification ConvertFromV20(this IHasDataSpecification embeddedDataSpecifications, AasxCompatibilityModels.AdminShellV20.HasDataSpecification sourceSpecification)
        {
            foreach (var sourceSpec in sourceSpecification)
            {
                var newEmbeddedSpec = new EmbeddedDataSpecification(null, null);
                newEmbeddedSpec.ConvertFromV20(sourceSpec);
                embeddedDataSpecifications.EmbeddedDataSpecifications.Add(newEmbeddedSpec);
            }

            return embeddedDataSpecifications;
        }
    }
}
