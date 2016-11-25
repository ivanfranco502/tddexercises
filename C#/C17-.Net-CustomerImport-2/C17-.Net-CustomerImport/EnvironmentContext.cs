using System;
using System.Configuration;
using com.tenpines.advancetdd;

namespace com.tenpines.advancetdd
{
    public abstract class EnvironmentContext
    {
        private static EnvironmentContext _currentEnvironmentContext;
        public static EnvironmentContext Current
        {
            get
            {
                if (_currentEnvironmentContext == null)
                {
                    switch (GetEnvironmentValue())
                    {
                        case "DEV":
                            _currentEnvironmentContext = new DevelopmentEnvironmentContext();
                            break;
                        case "UAT":
                            _currentEnvironmentContext = new IntegrationEnvironmentContext();
                            break;
                    }
                }
                return _currentEnvironmentContext;
            }
        }

        private static string GetEnvironmentValue()
        {
        
            return ConfigurationManager.AppSettings["Environment"];
        }

        public abstract ICustomerSystem CreateCustomerSystem();
        public abstract ISupplierSystem CreateSupplierSystem();
    }

    public class IntegrationEnvironmentContext : EnvironmentContext
    {
        public override ICustomerSystem CreateCustomerSystem()
        {
            return new PersistentCustomerSystem();
        }

        public override ISupplierSystem CreateSupplierSystem()
        {
            return new PersistentSupplierSystem();
        }
    }

    public class DevelopmentEnvironmentContext : EnvironmentContext
    {
        public override ICustomerSystem CreateCustomerSystem()
        {
            return new TransientCustomerSystem();
        }

        public override ISupplierSystem CreateSupplierSystem()
        {
            return new TransientSupplierSystem();
        }
    }
}