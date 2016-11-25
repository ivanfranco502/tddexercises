using NHibernate;

namespace com.tenpines.advancetdd
{
    public interface ICustomerSystem
    {
        Customer CustomerIdentifiedAs(string identificationType, string identificationNumber);
        ISession CreateSession();
        int NumberOfCustomers();
        void GetSession();
        void BeginTransaction();
        void Commit();
        void CloseSession();
        void AddCustomer(Customer newCustomer);
    }

    public interface ISupplierSystem
    {
        Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber);
        ISession CreateSession();
        int NumberOfSuppliers();
        void GetSession();
        void BeginTransaction();
        void Commit();
        void CloseSession();
        void AddSupplier(Supplier newSupplier);
        Customer CustomerIdentifiedAs(string anIdentificationType, string anIdentificationNumber);
    }

}