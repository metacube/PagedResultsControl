using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Novell.Directory.Ldap;

namespace PagedResultsControl
{
	public static class Program
	{
		public static void Main(string[] args)
		{
			var options = IntegrationOptions.DefaultIntegrationOptions;
			var pagedLdapHandler = new PagedResultsControlHandler<Employee>(ToEmployee);
			var employees = pagedLdapHandler.LoadAllPagedResults(options);

			var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var outputFilePath = Path.Combine(assemblyDirectory, "employees.json");
			File.WriteAllText(outputFilePath, JsonConvert.SerializeObject(employees));
			Debug.WriteLine($"Results serialized to <{outputFilePath}>");
		}
		
		// Replace this method with custom entity mapper.
		private static Employee ToEmployee([NotNull] LdapEntry entry)
		{
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			
			var employee = new Employee();
			var attributes = entry.getAttributeSet();
			foreach (LdapAttribute attribute in attributes)
			{
				if (attribute.Name == "sAMAccountName") employee.Login = attribute.StringValue;

				// toDo: handle multivalued attributes (just grab them all with)
				switch (attribute.Name)
				{
					case "sAMAccountName":
						employee.Login = attribute.StringValue;
						break;
					case "givenName":
						employee.Name = attribute.StringValue;
						break;
					case "sn":
						employee.Surname = attribute.StringValue;
						break;
					case "initials":
						employee.Initials = attribute.StringValue;
						break;
					case "department":
						employee.Department = attribute.StringValue;
						break;
					default:
						Debug.WriteLine($"Unexpected attribute: <{attribute.Name}>");
						break;
				}
			}

			return employee;
		}
	}
}