using System.Collections.Generic;
using System.Linq;
using NHibernate;

namespace com.tenpines.advancetdd
{
    public class TransientCustomerSystem : ICustomerSystem
    {
        private List<Customer> customers;

        public TransientCustomerSystem()
        {
        }

        public TransientCustomerSystem(List<Customer> customers)
        {
            this.customers = customers;
        }

        public Customer CustomerIdentifiedAs(string identificationType, string identificationNumber)
        {
            var customerFilter = customers.Where(c => c.IdentificationType == identificationType
                                                      && c.IdentificationNumber == identificationNumber).ToList();

            if (customerFilter.Count > 1)
                throw new System.Exception("There are more than once Customer with the given ID");
            if (customerFilter.Count == 0)
                throw new System.Exception("There is not exist a Customer with the given ID");

            return customerFilter[0];
        }

        public ISession CreateSession()
        {
            return null;
        }

        public int NumberOfCustomers()
        {
            return customers.Count;
        }

        public void GetSession()
        {
            customers = new List<Customer>();
        }

        public void BeginTransaction()
        {
        }

        public void Commit()
        {
        }

        public void CloseSession()
        {
        }

        public void AddCustomer(Customer newCustomer)
        {
            customers.Add(newCustomer);
        }
    }

    public class TransientSupplierSystem : ISupplierSystem
    {
        private List<Supplier> suppliers;
        private TransientCustomerSystem _customerSystem;
        public Supplier SupplierIdentifiedAs(string identificationType, string identificationNumber)
        {
            var supplierFilter = suppliers.Where(c => c.IdentificationType == identificationType
                                                      && c.IdentificationNumber == identificationNumber).ToList();

            if (supplierFilter.Count > 1)
                throw new System.Exception("There are more than once Customer with the given ID");
            if (supplierFilter.Count == 0)
                throw new System.Exception("There is not exist a Customer with the given ID");

            return supplierFilter[0];
        }

        public Customer CustomerIdentifiedAs(string anIdentificationType, string anIdentificationNumber)
        {
            List<Customer> customers = new List<Customer>();
            foreach (var supplier in suppliers)
                customers = customers.Union(supplier.Customers).ToList();

            _customerSystem = new TransientCustomerSystem(customers);
            return _customerSystem.CustomerIdentifiedAs(anIdentificationType, anIdentificationNumber);
        }

        public ISession CreateSession()
        {
            return null;
        }

        public int NumberOfSuppliers()
        {
            return suppliers.Count;
        }

        public void GetSession()
        {
            suppliers = new List<Supplier>();
        }

        public void BeginTransaction()
        {
        }

        public void Commit()
        {
        }

        public void CloseSession()
        {
        }

        public void AddSupplier(Supplier aSupplier)
        {
            suppliers.Add(aSupplier);
        }
    }

}
