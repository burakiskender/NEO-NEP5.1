using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace Neo.SmartContract
{
    public class ICO_Template : Framework.SmartContract
    {

        /*
                 Name: Gagapay network token

                Version: 1.0

                Author: Gagapay Limited

                Email: info@gagapay.com

                Description: Gagapay network token

                Symbol: GTA

                Precision: 8to

                Supply: 1,000,000,000
        */
        //Token Settings
        public static string Name() => "GagapayQwertv11 network token";
        public static string Symbol() => "GTA";
        public static readonly byte[] Owner = "Abdeg1wHpSrfjNzH5edGTabi5jdD9dvncX".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000; //decided by Decimals()
        private const ulong neo_decimals = 100000000;

        //ICO Settings
        private const ulong total_amount = 1000000000 * factor; // pre ico token amount

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;


        public static Object Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                if (Owner.Length == 20)
                {
                    // if param Owner is script hash
                    return Runtime.CheckWitness(Owner);
                }
                else if (Owner.Length == 33)
                {
                    // if param Owner is public key
                    byte[] signature = operation.AsByteArray();
                    return VerifySignature(signature, Owner);
                }
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (operation == "deploy") return Deploy();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "allowance") if (args.Length >= 2) return Allowance((byte[])args[0], (byte[])args[1]); else return 0;
                if (operation == "approve") if (args.Length >= 3) return Approve((byte[])args[0], (byte[])args[1], (BigInteger)args[2]); else return 0;
                if (operation == "transferFrom") if (args.Length >= 4) return TransferFrom((byte[])args[0], (byte[])args[1], (byte[])args[2], (BigInteger)args[3]); else return 0;
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
                if (operation == "decimals") return Decimals();
            }
            return false;
        }

        // initialization parameters, only once
        public static bool Deploy()
        {
            byte[] total_supply = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (total_supply.Length != 0) return false;
            Storage.Put(Storage.CurrentContext, Owner, total_amount);
            Storage.Put(Storage.CurrentContext, "totalSupply", total_amount);
            Transferred(null, Owner, total_amount);
            return true;
        }
        // get the total token supply
        // token
        public static BigInteger TotalSupply()
        {
            return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }
        /// <summary>
        ///   Checks the TransferFrom approval of two accounts.
        /// </summary>
        /// <param name="from">
        ///   The account which funds can be transfered from.
        /// </param>
        /// <param name="to">
        ///   The account which is granted usage of the account.
        /// </param>
        /// <returns>
        ///   The amount allocated for TransferFrom.
        /// </returns>
        public static BigInteger Allowance(byte[] from, byte[] to)
        {
            return Storage.Get(Storage.CurrentContext, from.Concat(to)).AsBigInteger();
        }
        /// <summary>
        ///   Approves another user to use the TransferFrom
        ///   function on the invoker's account.
        ///   TransferFrom 
        /// </summary>
        /// <param name="originator">
        ///   The contract invoker.
        /// </param>
        /// <param name="to">
        ///   The account to grant TransferFrom access to.
        ///   所批准的账户
        /// </param>
        /// <param name="amount">
        ///   The amount to grant TransferFrom access for.
        /// </param>
        /// <returns>
        ///   Transaction Successful?
        /// </returns>
        public static bool Approve(byte[] originator, byte[] to, BigInteger amount)
        {
            if (Runtime.CheckWitness(originator) && amount >=0)
            {
                Storage.Put(Storage.CurrentContext, originator.Concat(to), amount);
                return true;
            }
            return false;
        }
        /// <summary>
        ///   Transfers a balance from one account to another
        ///   on behalf of the account owner.
        /// </summary>
        /// <param name="originator">
        ///   The contract invoker.
        /// </param>
        /// <param name="from">
        ///   The account to transfer a balance from.
        /// </param>
        /// <param name="to">
        ///   The account to transfer a balance to.
        /// </param>
        /// <param name="amount">
        ///   The amount to transfer.
        /// </param>
        /// <returns>
        ///   Transaction successful?
        /// </returns>
        public static bool TransferFrom(byte[] originator, byte[] from, byte[] to, BigInteger amountToSend)
        {
            if (Runtime.CheckWitness(originator))
            {
                BigInteger allowedAmount = Storage.Get(Storage.CurrentContext, from.Concat(originator)).AsBigInteger();
                BigInteger ownerAmount = Storage.Get(Storage.CurrentContext, from).AsBigInteger();

                if (amountToSend >= 0 && ownerAmount >= 0 && allowedAmount >= amountToSend && ownerAmount >= amountToSend)
                {
                    BigInteger receiverAmount = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
                    Storage.Put(Storage.CurrentContext, from.Concat(originator), (allowedAmount - amountToSend));
                    Storage.Put(Storage.CurrentContext, from, (ownerAmount - amountToSend));
                    Storage.Put(Storage.CurrentContext, to, (receiverAmount + amountToSend));
                    Transferred(from, to, amountToSend);
                    return true;
                }
            }
            return false;
        }
        // function that is always called when someone wants to transfer tokens.
        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            if (!Runtime.CheckWitness(from)) return false;
            if (from == to) return true;
            BigInteger from_value = Storage.Get(Storage.CurrentContext, from).AsBigInteger();
            if (from_value < value) return false;
            if (from_value == value)
                Storage.Delete(Storage.CurrentContext, from);
            else
                Storage.Put(Storage.CurrentContext, from, from_value - value);
            BigInteger to_value = Storage.Get(Storage.CurrentContext, to).AsBigInteger();
            Storage.Put(Storage.CurrentContext, to, to_value + value);
            Transferred(from, to, value);
            return true;
        }

        // get the account balance of another account with address
        public static BigInteger BalanceOf(byte[] address)
        {
            return Storage.Get(Storage.CurrentContext, address).AsBigInteger();
        }

    }
}