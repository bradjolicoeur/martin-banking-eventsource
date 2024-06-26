﻿using Accounting.Events;
using Accounting.Projections;
using Marten;
using Marten.Events.Projections;
using System;
using Weasel.Core;

var store = DocumentStore.For(opts =>
{
    opts.Connection("host=localhost;database=marten_test;password=not_magical_scary;username=banking_user");

    opts.AutoCreateSchemaObjects = AutoCreate.All; //.All will wipe out the schema each time this is run

    opts.Projections.Add<AccountProjection>(ProjectionLifecycle.Inline); //Configure inline projection

});

// Establish Accounts
var khalid = new AccountCreated
{
    Owner = "Khalid Abuhakmeh",
    AccountId = Guid.NewGuid(),
    StartingBalance = 1000m
};

var bill = new AccountCreated
{
    Owner = "Bill Boga",
    AccountId = Guid.NewGuid()
};

// Create Accounts
using (var session = store.LightweightSession())
{
    // create banking accounts
    session.Events.StartStream(khalid.AccountId, khalid);
    session.Events.StartStream(bill.AccountId, bill);

    session.SaveChanges();
}

// First Transaction
using (var session = store.LightweightSession())
{
    // load khalid's account
    var account = session.Load<Account>(khalid.AccountId);
    // let's be generous
    var amount = 100m;
    var give = new AccountDebited
    {
        Amount = amount,
        To = bill.AccountId,
        From = khalid.AccountId,
        Description = "Bill helped me out with some code."
    };

    if (account.HasSufficientFunds(give))
    {
        session.Events.Append(give.From, give);
        session.Events.Append(give.To, give.ToCredit());
    }
    // commit these changes
    session.SaveChanges();
}

// Second Transaction
using (var session = store.LightweightSession())
{
    // load bill's account
    var account = session.Load<Account>(bill.AccountId);
    // let's try to over spend
    var amount = 1000m;
    var spend = new AccountDebited
    {
        Amount = amount,
        From = bill.AccountId,
        To = khalid.AccountId,
        Description = "Trying to buy that Ferrari"
    };

    if (account.HasSufficientFunds(spend))
    {
        // should not get here
        session.Events.Append(spend.From, spend);
        session.Events.Append(spend.To, spend.ToCredit());
    }
    else
    {
        session.Events.Append(account.Id, new InvalidOperationAttempted
        {
            Description = "Overdraft ("+ amount.ToString("C") + ")"
        });
    }
    // commit these changes
    session.SaveChanges();
}

// Query the Account Balance from Projection 
using (var session = store.LightweightSession())
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("----- Final Balance ------");

    var accounts = session.LoadMany<Account>(khalid.AccountId, bill.AccountId);

    foreach (var account in accounts)
    {
        Console.WriteLine(account);
    }
}

// List the account activity
using (var session = store.LightweightSession())
{
    foreach (var account in new[] { khalid, bill })
    {
        Console.WriteLine();
        Console.WriteLine($"Transaction ledger for {account.Owner}");
        var stream = session.Events.FetchStream(account.AccountId);
        foreach (var item in stream)
        {
            Console.WriteLine(item.Data);
        }
        Console.WriteLine();
    }
}

Console.ReadLine();
