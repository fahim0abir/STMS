import {
  Component,
  DestroyRef,
  Input,
  Output,
  EventEmitter,
  OnChanges,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Project } from '../../../../shared/models/project.model';
import { TaskItem, TaskItemUpsert } from '../../../../shared/models/task.model';
import { TASK_PRIORITY_LABELS, TASK_STATUS_LABELS, TaskPriority, TaskStatus } from '../../../../shared/models/enums';
import { AiService, AI_MAX_LENGTH } from '../../../../core/services/ai.service';
import { ButtonComponent } from '../../../../shared/ui/button.component';

export interface TaskModalSaveEvent {
  dto: TaskItemUpsert;
  projectId?: string;
}

import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ButtonComponent],
  templateUrl: './task-modal.component.html',
  styleUrl: './task-modal.component.scss',
})
export class TaskModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly aiService = inject(AiService);
  private readonly destroyRef = inject(DestroyRef);

  @Input() open = false;
  @Input() task: TaskItem | null = null;
  @Input() projects: Project[] = [];
  @Input() saving = false;
  @Input() errorMessage = '';

  @Output() close = new EventEmitter<void>();
  @Output() save = new EventEmitter<TaskModalSaveEvent>();

  readonly aiLoading = signal(false);
  readonly aiMessage = signal('');
  readonly aiUsedFallback = signal(false);
  readonly aiSuggestion = signal<string | null>(null);
  readonly canRevert = signal(false);

  private descriptionBeforeAccept: string | null = null;

  readonly statusOptions = Object.entries(TASK_STATUS_LABELS).map(([value, label]) => ({
    value: Number(value) as TaskStatus,
    label,
  }));

  readonly priorityOptions = Object.entries(TASK_PRIORITY_LABELS).map(([value, label]) => ({
    value: Number(value) as TaskPriority,
    label,
  }));

  readonly maxDescriptionLength = AI_MAX_LENGTH;

  readonly form = this.fb.nonNullable.group({
    projectId: [''],
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.maxLength(AI_MAX_LENGTH)]],
    status: [TaskStatus.ToDo as TaskStatus, [Validators.required]],
    priority: [TaskPriority.Medium as TaskPriority, [Validators.required]],
  });

  get isEdit(): boolean {
    return !!this.task;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['open']?.currentValue === true || changes['task']) {
      this.resetState();
      this.form.reset({
        projectId: '',
        title: this.task?.title ?? '',
        description: this.task?.description ?? '',
        status: this.task?.status ?? TaskStatus.ToDo,
        priority: this.task?.priority ?? TaskPriority.Medium,
      });
    }
  }

  improveWithAi(): void {
    const description = this.form.controls.description.value;
    const validationError = this.aiService.validate(description);
    this.aiSuggestion.set(null);
    this.aiUsedFallback.set(false);

    if (validationError) {
      this.aiMessage.set(validationError);
      return;
    }

    this.aiLoading.set(true);
    this.aiMessage.set('');
    this.aiService.enhance(description).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result) => {
        this.aiLoading.set(false);
        this.aiUsedFallback.set(result.usedFallback);
        this.aiMessage.set(result.message ?? '');
        this.aiSuggestion.set(result.usedFallback ? null : result.text);
      },
      error: (err) => {
        this.aiLoading.set(false);
        this.aiMessage.set(err?.message || 'AI enhancement failed.');
      },
    });
  }

  acceptSuggestion(): void {
    const suggestion = this.aiSuggestion();
    if (!suggestion) return;
    this.descriptionBeforeAccept = this.form.controls.description.value;
    this.form.controls.description.setValue(suggestion);
    this.aiSuggestion.set(null);
    this.canRevert.set(true);
    this.aiMessage.set('AI suggestion applied.');
  }

  rejectSuggestion(): void {
    this.aiSuggestion.set(null);
    this.aiMessage.set('');
  }

  revertDescription(): void {
    if (this.descriptionBeforeAccept === null) return;
    this.form.controls.description.setValue(this.descriptionBeforeAccept);
    this.descriptionBeforeAccept = null;
    this.canRevert.set(false);
    this.aiMessage.set('Reverted to your original description.');
  }

  submit(): void {
    if (this.saving) return;

    if (!this.isEdit && !this.form.controls.projectId.value) {
      this.aiMessage.set('Select a project for the new task.');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const dto: TaskItemUpsert = {
      title: raw.title.trim(),
      description: raw.description?.trim() || null,
      status: Number(raw.status) as TaskStatus,
      priority: Number(raw.priority) as TaskPriority,
    };

    this.save.emit({
      dto,
      projectId: this.isEdit ? undefined : raw.projectId,
    });
  }

  private resetState(): void {
    this.aiSuggestion.set(null);
    this.aiMessage.set('');
    this.aiUsedFallback.set(false);
    this.aiLoading.set(false);
    this.canRevert.set(false);
    this.descriptionBeforeAccept = null;
  }
}

