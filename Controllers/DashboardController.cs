using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Dashboard.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            // Last 7 Days
            DateTime StartDate = DateTime.Today.AddDays(-6);
            DateTime EndDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(y => y.Date >= StartDate && y.Date <= EndDate)
                .ToListAsync();

            // Calculate Total Income in Rand
            decimal TotalIncomeZAR = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);

            ViewBag.TotalIncome = TotalIncomeZAR.ToString("C0", CultureInfo.GetCultureInfo("en-ZA"));

            // Calculate Total Expense in Rand
            decimal TotalExpenseZAR = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);

            ViewBag.TotalExpense = TotalExpenseZAR.ToString("C0", CultureInfo.GetCultureInfo("en-ZA"));

            // Calculate Balance in Rand
            decimal BalanceZAR = TotalIncomeZAR - TotalExpenseZAR;
            ViewBag.Balance = BalanceZAR.ToString("C0", CultureInfo.GetCultureInfo("en-ZA"));

            // Doughnut Chart - Expense By Category
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j => j.Amount).ToString("C0", new CultureInfo("en-ZA")),
                })
                .OrderByDescending(l => l.amount)
                .ToList();

            // Spline Chart - Income vs Expense
            List<SplineChartData> IncomeExpenseData = (from day in Enumerable.Range(0, 7)
                                                       select new SplineChartData
                                                       {
                                                           Day = StartDate.AddDays(day).ToString("dd-MMM"),
                                                           Income = SelectedTransactions
                                                               .Where(i => i.Category.Type == "Income" && i.Date.Date == StartDate.AddDays(day).Date)
                                                               .Sum(j => j.Amount),
                                                           Expense = SelectedTransactions
                                                               .Where(i => i.Category.Type == "Expense" && i.Date.Date == StartDate.AddDays(day).Date)
                                                               .Sum(j => j.Amount)
                                                       }).ToList();

            ViewBag.SplineChartData = IncomeExpenseData;

            // Recent Transactions (No changes needed for currency)

            ViewBag.RecentTransactions = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }

        public class SplineChartData
        {
            public string Day { get; set; }
            public decimal Income { get; set; }
            public decimal Expense { get; set; }
        }
    }
}
