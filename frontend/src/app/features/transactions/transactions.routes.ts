import { Routes } from '@angular/router';

export const TRANSACTIONS_ROUTES: Routes = [
  {
    path: '',
    title: 'Transactions',
    loadComponent: () =>
      import('./pages/transaction-list-page/transaction-list-page').then((m) => m.TransactionListPage),
  },
  {
    path: ':id',
    title: 'Transaction details',
    loadComponent: () =>
      import('./pages/transaction-detail-page/transaction-detail-page').then((m) => m.TransactionDetailPage),
  },
];
