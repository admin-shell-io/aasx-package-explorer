namespace Extensions
{
    public static class ExtendKeyTypes
    {
        public static bool IsSME(this KeyTypes keyType)
        {
            foreach (var kt in Constants.AasSubmodelElementsAsKeys)
                if (kt.HasValue && kt.Value == keyType)
                    return true;
            return false;
        }
    }
}
