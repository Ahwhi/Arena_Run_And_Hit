namespace Server.Infra {
    public static class Store {
        public static StoreCatalog Catalog { get; internal set; } = new StoreCatalog { Version = 0 };
    }
}
