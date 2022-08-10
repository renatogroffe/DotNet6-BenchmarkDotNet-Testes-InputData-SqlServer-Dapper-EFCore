using Microsoft.Data.SqlClient;
using BenchmarkDotNet.Attributes;
using Bogus.DataSets;
using Bogus.Extensions.Brazil;
using Dapper.Contrib.Extensions;
using BenchmarkingDapperEFCoreCRM.EFCore;
using BenchmarkingDapperEFCoreCRM.Dapper;

namespace BenchmarkingDapperEFCoreCRM.Tests;

[SimpleJob(BenchmarkDotNet.Engines.RunStrategy.Throughput, launchCount: 5)]
public class CRMTests
{
    #region EFCore Tests
    
    private CRMContext? _context;
    private Name? _namesDataSetEF;
    private PhoneNumbers? _phonesDataSetEF;
    private Address? _addressesDataSetEF;
    private Company? _companiesDataSetEF;

    [IterationSetup(Target = nameof(InputDataWithEntityFrameworkCore))]
    public void SetupEntityFrameworkCore()
    {
        _context = new CRMContext();
        _namesDataSetEF = new Name("pt_BR");
        _phonesDataSetEF = new PhoneNumbers("pt_BR");
        _addressesDataSetEF = new Address("pt_BR");
        _companiesDataSetEF = new Company("pt_BR");
    }

    [Benchmark]
    public EFCore.Empresa InputDataWithEntityFrameworkCore()
    {
        var empresa = new EFCore.Empresa()
        {
            Nome = _companiesDataSetEF!.CompanyName(),
            CNPJ = _companiesDataSetEF!.Cnpj(includeFormatSymbols: false),
            Cidade = _addressesDataSetEF!.City(),
            Contatos = new ()
            {
                new ()
                {
                    Nome = _namesDataSetEF!.FullName(),
                    Telefone = _phonesDataSetEF!.PhoneNumber()
                }
            }
        };

        _context!.Add(empresa);
        _context!.SaveChanges();

        return empresa;        
    }

    [IterationCleanup(Target = nameof(InputDataWithEntityFrameworkCore))]
    public void CleanupEntityFrameworkCore()
    {
        _context = null;
    }

    #endregion

    #region Dapper Tests

    private SqlConnection? _connection;
    private Name? _namesDataSetDapper;
    private PhoneNumbers? _phonesDataSetDapper;
    private Address? _addressesDataSetDapper;
    private Company? _companiesDataSetDapper;

    [IterationSetup(Target = nameof(InputDataWithDapper))]
    public void SetupDapper()
    {
        _connection = new SqlConnection(Configurations.BaseDapper);
        _namesDataSetDapper = new Name("pt_BR");
        _phonesDataSetDapper = new PhoneNumbers("pt_BR");
        _addressesDataSetDapper = new Address("pt_BR");
        _companiesDataSetDapper = new Company("pt_BR");
    }

    [Benchmark]
    public Dapper.Empresa InputDataWithDapper()
    {
        var empresa = new Dapper.Empresa()
        {
            Nome = _companiesDataSetDapper!.CompanyName(),
            CNPJ = _companiesDataSetDapper!.Cnpj(includeFormatSymbols: false),
            Cidade = _addressesDataSetDapper!.City()
        };

        var contato = new Dapper.Contato()
        {
            Nome = _namesDataSetDapper!.FullName(),
            Telefone = _phonesDataSetDapper!.PhoneNumber()
        };
        
        _connection!.Open();
        var transaction = _connection.BeginTransaction();

        _connection.Insert<Dapper.Empresa>(empresa, transaction);
        
        contato.IdEmpresa = empresa.IdEmpresa;
        _connection.Insert<Dapper.Contato>(contato, transaction);

        transaction.Commit();
        _connection.Close();
        
        empresa.Contatos = new () { contato };
        return empresa;
    }

    [IterationCleanup(Target = nameof(InputDataWithDapper))]
    public void CleanupDapper()
    {
        _connection = null;
    }

    #endregion
}