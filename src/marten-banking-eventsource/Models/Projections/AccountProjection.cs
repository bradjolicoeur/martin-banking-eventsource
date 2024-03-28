using Accounting.Events;
using System;
using Marten.Events.Aggregation;

namespace Accounting.Projections
{
    public class AccountProjection : SingleStreamProjection<Account>
    {
        public Account Create(AccountCreated created)
        {
            Account account = new Account();
            account.Id = created.AccountId;
            account.Owner = created.Owner;
            account.Balance = created.StartingBalance;
            account.CreatedAt = account.UpdatedAt = created.CreatedAt;

            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Account created for {account.Owner} with Balance of {account.Balance.ToString("C")}");
            return account;
        }

        public void Apply(AccountDebited debit,Account account)
        {
            debit.Apply(account);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Debiting {account.Owner} ({debit.Amount.ToString("C")}): {debit.Description}");
        }

        public void Apply(AccountCredited credit, Account account)
        {
            credit.Apply(account);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Crediting {account.Owner} {credit.Amount.ToString("C")}: {credit.Description}");
        }

        
    }
}
