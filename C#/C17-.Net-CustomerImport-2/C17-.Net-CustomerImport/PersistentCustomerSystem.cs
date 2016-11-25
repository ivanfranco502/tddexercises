using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Tool.hbm2ddl;

namespace com.tenpines.advancetdd
{
    public class PersistentCustomerSystem : ICustomerSystem
    {
        private ISession _session;
        private ITransaction _transaction;

        public PersistentCustomerSystem()
        {
        }

        public PersistentCustomerSystem(ISession _session)
        {
            this._session = _session;
        }

        public Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customers = this._session.CreateCriteria<Customer>().
                Add(Restrictions.Eq("IdentificationType", identificationType)).
                Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Customer>();

            if (customers.Count > 1)
                throw new System.Exception("There are more than once Customer with the given ID");
            if (customers.Count == 0)
                throw new System.Exception("There is not exist a Customer with the given ID");

            return customers[0];
        }

        public ISession CreateSession()
        {
            var storeConfiguration = new StoreConfiguration();
            var configuration = Fluently.Configure()
                .Database(
                    MsSqlCeConfiguration.Standard.ShowSql()
                        .ConnectionString("Data Source=DB.sdf"))
                .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Customer>(storeConfiguration)
                    .Override<Customer>(
                        map =>
                                map.HasMany(x => x.Addresses).Cascade.All())));

            var sessionFactory = configuration.BuildSessionFactory();
            new SchemaExport(configuration.BuildConfiguration()).Execute(true, true, false);
            var session = sessionFactory.OpenSession();
            return session;
        }

        public int NumberOfCustomers()
        {
            return this._session.CreateCriteria<Customer>().List<Customer>().Count;
        }

        public void GetSession()
        {
            this._session = this.CreateSession();
        }

        public void BeginTransaction()
        {
            this._transaction = this._session.BeginTransaction();
        }

        public void Commit()
        {
            this._transaction.Commit();
        }

        public void CloseSession()
        {
            this._session.Close();
        }

        public void AddCustomer(Customer newCustomer)
        {
            this._session.Persist(newCustomer);
        }
    }

    public class PersistentSupplierSystem : ISupplierSystem
    {
        private ISession _session;
        private ITransaction _transaction;
        private PersistentCustomerSystem _customerSystem;
        public Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber)
        {
            var supplier = this._session.CreateCriteria<Supplier>().
                Add(Restrictions.Eq("IdentificationType", identificationType)).
                Add(Restrictions.Eq("IdentificationNumber", identificationNumber)).List<Supplier>();

            if (supplier.Count > 1)
                throw new System.Exception("There are more than once Suppliers with the given ID");
            if (supplier.Count == 0)
                throw new System.Exception("There is not exist a Supplier with the given ID");

            return supplier[0];
        }

        public Customer CustomerIdentifiedAs(string anIdentificationType, string anIdentificationNumber)
        {
            _customerSystem = new PersistentCustomerSystem(_session);

            return _customerSystem.CustomerIdentifiedAs(anIdentificationType, anIdentificationNumber);
        }

        public ISession CreateSession()
        {
            var storeConfiguration = new StoreConfiguration();
            var configuration = Fluently.Configure()
                .Database(
                    MsSqlCeConfiguration.Standard.ShowSql()
                        .ConnectionString("Data Source=DB.sdf"))
                .Mappings(m => m.AutoMappings.Add(AutoMap.AssemblyOf<Supplier>(storeConfiguration)
                    .Override<Supplier>(
                        map => map.HasMany(x => x.Addresses).Cascade.All())
                    .Override<Supplier>(
                        map => map.HasMany(x => x.Customers).Cascade.All())));

            var sessionFactory = configuration.BuildSessionFactory();
            new SchemaExport(configuration.BuildConfiguration()).Execute(true, true, false);
            var session = sessionFactory.OpenSession();
            return session;
        }

        public int NumberOfSuppliers()
        {
            return this._session.CreateCriteria<Supplier>().List<Supplier>().Count;
        }

        public void GetSession()
        {
            this._session = this.CreateSession();
        }

        public void BeginTransaction()
        {
            this._transaction = this._session.BeginTransaction();
        }

        public void Commit()
        {
            this._transaction.Commit();
        }

        public void CloseSession()
        {
            this._session.Close();
        }

        public void AddSupplier(Supplier newSupplier)
        {
            this._session.Persist(newSupplier);
        }
    }
}