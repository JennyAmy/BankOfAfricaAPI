﻿using AutoMapper;
using BankOfAfricaAPI.DTOs.BankDTO;
using BankOfAfricaAPI.Entities;
using BankOfAfricaAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BankOfAfricaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController : BaseController
    {
        private readonly IMapper mapper;
        private readonly IUnitOfWork unitOfWork;

        public TransactionController(IMapper mapper, IUnitOfWork unitOfWork)
        {
            this.mapper = mapper;
            this.unitOfWork = unitOfWork;
        }


        [HttpPut("transfer")]
        public async Task<IActionResult> Transfer(decimal amount, string accountNo, TransactionDTO transactionDTO)
        {
            //var loggedInUserId = GetUserId();
            var loggedInUserId = 1;
            var sender = await unitOfWork.BankAccountRepository.GetAccountDetailsById(loggedInUserId);
            var receiver = await unitOfWork.BankAccountRepository.GetAccountDetailsByAccountNo(accountNo);
            var transaction = mapper.Map<Transaction>(transactionDTO);
 
            if(accountNo != sender.AccountNumber)
            {
                if (amount < sender.AccountBal) //Check if the sender's account bal is less than amount about to be sent
                {
                    //First debit the user
                    sender.AccountBal = sender.AccountBal - amount;
                    sender.LastUpdatedBy = loggedInUserId;
                    sender.LastUpdatedOn = DateTime.Now;

                    //Then credit the user
                    receiver.AccountBal = receiver.AccountBal + amount;
                    receiver.LastUpdatedOn = DateTime.Now;
                    receiver.LastUpdatedBy = loggedInUserId;

                    //Log the transaction in transactions table
                    transaction.SenderId = loggedInUserId;
                    transaction.ReceiverId = receiver.CustomerId;
                    transaction.SenderAccountNo = sender.AccountNumber;
                    transaction.ReceiverAccountNo = accountNo;
                    transaction.AmountSent = amount;

                    //mapper.Map(transactionDTO, transaction);
                    unitOfWork.BankAccountRepository.AddTransaction(transaction);
                }
                else
                {
                    return BadRequest("Insufficient balance to carry out this transaction");
                }
            }
            else
            {
                return BadRequest("You cannot transfer money to your own account");
            }
              
            await unitOfWork.SaveAsync();
            return Ok("Transfer of " + amount + " successfully transferred to " + receiver.Firstname + " " + receiver.Surname);
        }
    }
}
