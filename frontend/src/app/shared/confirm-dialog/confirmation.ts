import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialog, ConfirmDialogData } from './confirm-dialog';

@Injectable({ providedIn: 'root' })
export class Confirmation {
  private readonly dialog = inject(MatDialog);

  async ask(data: ConfirmDialogData): Promise<boolean> {
    const reference = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data,
      autoFocus: 'dialog',
    });
    return (await firstValueFrom(reference.afterClosed())) === true;
  }
}
