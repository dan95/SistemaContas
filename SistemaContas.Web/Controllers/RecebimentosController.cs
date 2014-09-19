﻿using CadastroContas.Core.Dominio.Dados.Contrato;
using Ninject;
using SistemaContas.Core.Dominio.Dados.Contrato;
using SistemaContas.Core.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SistemaContas.Web.Controllers
{
    [OutputCache(NoStore = true, Duration = 60, VaryByParam = "*")]
    public class RecebimentosController : Controller
    {
        [Inject]
        public IMovimentacaoRepository repositoryPagamento { get; set; }

        [Inject]
        public IContaRepository repositoryConta { get; set; }

        [Inject]
        public IClienteRepository repositoryCliente { get; set; }

        [Inject]
        public ITransacaoRepository repositoryTransacao { get; set; }



        [Authorize(Roles = "Usuario, Administrador")]
        public ActionResult CadastrarRecebimento(int id)
        {
            var conta = repositoryConta.PegarContaPorId(id);
            if (conta.StatusConta == "Em andamento" || conta.StatusConta == "Finalizada")
            {
                return RedirectToAction("Index", "Contas");
            }
            else
            {
                List<string> lista = new List<string>();
                for (int i = 1; i < 6; i++)
                {
                    lista.Add(i.ToString());
                }
                SelectList numeroParcelas = new SelectList(lista);
                ViewBag.ListaParcelas = numeroParcelas;
                Movimentacao recebimento = new Movimentacao();
                recebimento.Conta = repositoryConta.PegarContaPorId(id);
                return View();
            }
        }

        [Authorize(Roles = "Usuario, Administrador"), AcceptVerbs(HttpVerbs.Post)]
        public ActionResult CadastrarRecebimento(Movimentacao recebimento, int id = 0)
        {
            var conta = repositoryConta.PegarContaPorId(id);
            recebimento.Conta = conta;
            recebimento.Cliente = repositoryCliente.PegarClientePorId(Convert.ToInt32(User.Identity.Name));
            recebimento.Valor = recebimento.Conta.ValorConta;
            recebimento.ValorParcela = recebimento.Valor / recebimento.NumeroParcelas;
            recebimento.ParcelasRestantes = recebimento.NumeroParcelas;
            recebimento.Tipo = recebimento.Conta.TipoOperacao;
            recebimento.Status = "Em aberto";
            recebimento.ValorRestante = recebimento.Valor;
            recebimento.UltimaAtualizacao = DateTime.Now;
            conta.StatusConta = "Em andamento";
            repositoryConta.AtualizarConta(conta);
            repositoryPagamento.SalvarMovimentacao(recebimento);
            return RedirectToAction("EmAndamento", "Pendencias");
        }

        [Authorize(Roles = "Usuario, Administrador")]
        public ActionResult Parcela(int id = 0)
        {

            var conta = repositoryConta.PegarContaPorId(id);
            if (id != 0 && conta != null)
            {
                var recebimento = repositoryPagamento.PegarMovimentacao(id);
                recebimento.Conta = repositoryConta.PegarContaPorId(id);
                return View(recebimento);
            }
            else
            {
                return RedirectToAction("Index", "Contas");
            }
        }

        [Authorize(Roles = "Usuario, Administrador"), AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Parcela(Movimentacao recebimento, int id = 0)
        {
            var conta = repositoryConta.PegarContaPorId(id);
            if (id != 0 && conta != null)
            {
                recebimento = repositoryPagamento.PegarMovimentacao(id);

                recebimento.Conta = conta;
                recebimento.Cliente = repositoryCliente.PegarClientePorId(Convert.ToInt32(User.Identity.Name));
                recebimento.ValorRestante = recebimento.ValorRestante - recebimento.ValorParcela;
                recebimento.ParcelasRestantes -= 1;
                recebimento.Status = "Em andamento";
                if (recebimento.ParcelasRestantes == 0)
                {
                    recebimento.Status = "Finalizada";
                    conta.StatusConta = "Finalizada";
                    repositoryConta.AtualizarConta(conta);
                }
                recebimento.UltimaAtualizacao = DateTime.Now;

                Transacao t = new Transacao();
                t.Cliente = recebimento.Cliente;
                t.Conta = recebimento.Conta;
                t.DataTransacao = recebimento.UltimaAtualizacao;
                t.ValorTransacao = recebimento.ValorParcela;
                t.Movimentacao = recebimento;
                t.TipoTransacao = recebimento.Tipo;

                repositoryTransacao.SalvarTransacao(t);
                repositoryPagamento.SalvarOuAtualizarMovimentacao(recebimento);

                return RedirectToAction("EmAndamento", "Pendencias");

            }
            else
            {
                return RedirectToAction("Index", "Contas");
            }
        }

        [Authorize(Roles = "Usuario, Administrador")]
        public ActionResult QuitarRecebimento(int id = 0)
        {

            var conta = repositoryConta.PegarContaPorId(id);
            if (id != 0 && conta != null)
            {
                var recebimento = repositoryPagamento.PegarMovimentacao(id);
                recebimento.Conta = repositoryConta.PegarContaPorId(id);
                return View(recebimento);
            }
            else
            {
                return RedirectToAction("Index", "Contas");
            }
        }

        [Authorize(Roles = "Usuario, Administrador"), AcceptVerbs(HttpVerbs.Post)]
        public ActionResult QuitarRecebimento(Movimentacao recebimento, int id = 0)
        {
            var conta = repositoryConta.PegarContaPorId(id);
            if (id != 0 && conta != null)
            {
                recebimento = repositoryPagamento.PegarMovimentacao(id);

                recebimento.Conta = conta;
                recebimento.Cliente = repositoryCliente.PegarClientePorId(Convert.ToInt32(User.Identity.Name));
                var valor = recebimento.ValorRestante;
                recebimento.ValorRestante = 0;
                recebimento.ParcelasRestantes = 0;
                recebimento.Status = "Em andamento";
                if (recebimento.ParcelasRestantes == 0)
                {
                    recebimento.Status = "Finalizada";
                    conta.StatusConta = "Finalizada";
                    repositoryConta.AtualizarConta(conta);
                }
                recebimento.UltimaAtualizacao = DateTime.Now;

                Transacao t = new Transacao();
                t.Cliente = recebimento.Cliente;
                t.Conta = recebimento.Conta;
                t.DataTransacao = recebimento.UltimaAtualizacao;
                t.ValorTransacao = valor;
                t.Movimentacao = recebimento;
                t.TipoTransacao = recebimento.Tipo;

                repositoryTransacao.SalvarTransacao(t);
                repositoryPagamento.SalvarOuAtualizarMovimentacao(recebimento);

                return RedirectToAction("EmAndamento", "Pendencias");

            }
            else
            {
                return RedirectToAction("Index", "Contas");
            }
        }
    }
}