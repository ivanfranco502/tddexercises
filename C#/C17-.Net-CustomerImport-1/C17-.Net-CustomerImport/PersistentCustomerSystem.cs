using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Tool.hbm2ddl;

namespace com.tenpines.advancetdd
{
    public class PersistentCustomerSystem
    {
        private ISession _session;

        public ISession CreateSession()
        {
            var storeConfiguration = new StoreConfiguration();
            var configuration = Fluently.Configure()
                .Database(
                    MsSqlCeConfiguration.Standard.ShowSql()
                        .ConnectionString("Data Source=CustomerImport.sdf"))
                .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Customer>(storeConfiguration)
                    .Override<Customer>(
                        map =>
                                map.HasMany(x => x.Addresses).Cascade.All())));

            var sessionFactory = configuration.BuildSessionFactory();
            new SchemaExport(configuration.BuildConfiguration()).Execute(true, true, false);
            var session = sessionFactory.OpenSession();
            return session;
        }

        public Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customers = this._session.CreateCriteria<Customer>().
                Add(Restrictions.Eq("IdentificationType", identificationType)).
                Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Customer>();
            Assert.AreEqual(1, customers.Count);

            return customers[0];
        }
    }
}